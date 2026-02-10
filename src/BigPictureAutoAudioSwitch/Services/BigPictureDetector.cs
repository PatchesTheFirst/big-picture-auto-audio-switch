using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BigPictureAutoAudioSwitch.Services;

public class BigPictureDetector : IBigPictureDetector, IDisposable
{
    private const string BigPictureWindowClass = "SDL_app";
    private const string BigPictureWindowTitle = "Steam Big Picture Mode";
    
    private const uint EVENT_OBJECT_CREATE = 0x8000;
    private const uint EVENT_OBJECT_DESTROY = 0x8001;
    private const uint EVENT_OBJECT_SHOW = 0x8002;
    private const uint EVENT_OBJECT_HIDE = 0x8003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    
    private readonly IAudioService _audioService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BigPictureDetector> _logger;
    
    private IntPtr _hookCreate;
    private IntPtr _hookDestroy;
    private IntPtr _hookShow;
    private IntPtr _hookHide;
    private WinEventDelegate? _winEventDelegate;
    private GCHandle _delegateHandle;
    private bool _disposed;
    
    // Debounce to prevent rapid activation after deactivation
    private DateTime _lastDeactivationTime = DateTime.MinValue;
    private static TimeSpan DeactivationCooldown => AppConstants.DeactivationCooldown;
    
    // Track the active Big Picture window handle for deactivation detection
    private IntPtr _activeBigPictureHandle = IntPtr.Zero;
    
    // Background retry for failed device switches
    private CancellationTokenSource? _retryCts;

    public bool IsBigPictureActive { get; private set; }

    public event EventHandler<bool>? BigPictureStateChanged;

    public BigPictureDetector(
        IAudioService audioService, 
        ISettingsService settingsService,
        INotificationService notificationService,
        ILogger<BigPictureDetector> logger)
    {
        _audioService = audioService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public void Start()
    {
        _logger.LogInformation("Starting Big Picture detector");
        
        // Check if already running at startup
        IsBigPictureActive = IsBigPictureWindowPresent();
        if (IsBigPictureActive)
        {
            _logger.LogInformation("Big Picture Mode already active at startup");
            _ = OnBigPictureActivatedAsync();
        }

        // Set up event hooks - pin the delegate to prevent GC
        _winEventDelegate = new WinEventDelegate(WinEventProc);
        _delegateHandle = GCHandle.Alloc(_winEventDelegate);
        
        _hookCreate = SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, 
            IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        
        _hookDestroy = SetWinEventHook(EVENT_OBJECT_DESTROY, EVENT_OBJECT_DESTROY, 
            IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
            
        _hookShow = SetWinEventHook(EVENT_OBJECT_SHOW, EVENT_OBJECT_SHOW, 
            IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
            
        _hookHide = SetWinEventHook(EVENT_OBJECT_HIDE, EVENT_OBJECT_HIDE, 
            IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    public void Stop()
    {
        _logger.LogInformation("Stopping Big Picture detector");
        
        if (_hookCreate != IntPtr.Zero)
        {
            UnhookWinEvent(_hookCreate);
            _hookCreate = IntPtr.Zero;
        }
        
        if (_hookDestroy != IntPtr.Zero)
        {
            UnhookWinEvent(_hookDestroy);
            _hookDestroy = IntPtr.Zero;
        }
        
        if (_hookShow != IntPtr.Zero)
        {
            UnhookWinEvent(_hookShow);
            _hookShow = IntPtr.Zero;
        }
        
        if (_hookHide != IntPtr.Zero)
        {
            UnhookWinEvent(_hookHide);
            _hookHide = IntPtr.Zero;
        }
        
        // Free the pinned delegate
        if (_delegateHandle.IsAllocated)
        {
            _delegateHandle.Free();
        }
    }

    private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, 
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        // Only process window events (idObject == 0)
        if (idObject != 0) return;

        var className = GetWindowClassName(hwnd);
        if (className != BigPictureWindowClass) return;

        var windowTitle = GetWindowTitle(hwnd);

        switch (eventType)
        {
            case EVENT_OBJECT_CREATE:
            case EVENT_OBJECT_SHOW:
                // Check for the exact Big Picture Mode window title
                if (!windowTitle.Equals(BigPictureWindowTitle, StringComparison.OrdinalIgnoreCase))
                    return;

                // Verify it's from Steam process
                if (!IsWindowFromSteam(hwnd))
                    return;
                
                if (!IsBigPictureActive)
                {
                    // Check cooldown to prevent false activation right after deactivation
                    if (DateTime.UtcNow - _lastDeactivationTime < DeactivationCooldown)
                    {
                        _logger.LogDebug("Ignoring Big Picture activation within cooldown period ({Cooldown}ms)", DeactivationCooldown.TotalMilliseconds);
                        return;
                    }
                    
                    _logger.LogInformation("Steam Big Picture Mode activated - triggering audio device switch");
                    IsBigPictureActive = true;
                    _activeBigPictureHandle = hwnd;
                    _ = OnBigPictureActivatedAsync();
                    BigPictureStateChanged?.Invoke(this, true);
                }
                break;
                
            case EVENT_OBJECT_DESTROY:
            case EVENT_OBJECT_HIDE:
                // For deactivation, verify the Big Picture window is gone
                // (title might be empty during destruction, so we check by FindWindow)
                if (!IsBigPictureActive)
                    return;
                
                if (!IsBigPictureWindowPresent())
                {
                    _logger.LogInformation("Steam Big Picture Mode closed - restoring previous audio device");
                    IsBigPictureActive = false;
                    _activeBigPictureHandle = IntPtr.Zero;
                    _lastDeactivationTime = DateTime.UtcNow;
                    OnBigPictureDeactivated();
                    BigPictureStateChanged?.Invoke(this, false);
                }
                break;
        }
    }

    private async Task OnBigPictureActivatedAsync()
    {
        try
        {
            // Cancel any existing retry
            _retryCts?.Cancel();
            _retryCts?.Dispose();
            _retryCts = new CancellationTokenSource();
            
            _audioService.StoreCurrentDevice();
            var success = await _audioService.SwitchToTargetDeviceAsync();
            
            if (success)
            {
                if (_settingsService.Settings.ShowNotifications)
                {
                    var device = _audioService.GetDefaultDevice();
                    _notificationService.ShowAudioSwitched(device?.FullName ?? "Unknown", true);
                }
            }
            else
            {
                // Start background retry for devices that need time to initialize (e.g., HDMI)
                _logger.LogInformation("Initial audio switch failed, starting background retry");
                _ = RetryDeviceSwitchAsync(_retryCts.Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Big Picture activation audio switch");
        }
    }

    private async Task RetryDeviceSwitchAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting background retry for audio device switch");
        
        for (int i = 0; i < AppConstants.BackgroundRetryMaxAttempts; i++)
        {
            try
            {
                await Task.Delay(AppConstants.BackgroundRetryDelayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Background retry cancelled");
                return;
            }
            
            if (!IsBigPictureActive)
            {
                _logger.LogDebug("Big Picture closed, stopping retry");
                return;
            }
            
            try
            {
                var success = await _audioService.SwitchToTargetDeviceAsync(cancellationToken);
                if (success)
                {
                    _logger.LogInformation("Background retry succeeded on attempt {Attempt}", i + 1);
                    if (_settingsService.Settings.ShowNotifications)
                    {
                        var device = _audioService.GetDefaultDevice();
                        _notificationService.ShowAudioSwitched(device?.FullName ?? "Unknown", true);
                    }
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Background retry cancelled during switch");
                return;
            }
        }
        
        _logger.LogWarning("Background retry exhausted after {Attempts} attempts", 
            AppConstants.BackgroundRetryMaxAttempts);
        _notificationService.ShowDeviceMissing("configured audio device");
    }

    private void OnBigPictureDeactivated()
    {
        try
        {
            // Cancel any background retry
            _retryCts?.Cancel();
            
            _logger.LogInformation("Big Picture Mode closed, restoring previous audio device");
            _audioService.RestoreStoredDevice();
            
            if (_settingsService.Settings.ShowNotifications)
            {
                var device = _audioService.GetDefaultDevice();
                _notificationService.ShowAudioSwitched(device?.FullName ?? "Unknown", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Big Picture deactivation audio restoration");
        }
    }

    private bool IsBigPictureWindowPresent()
    {
        // Look for the specific Big Picture Mode window by title
        var hwnd = FindWindow(BigPictureWindowClass, BigPictureWindowTitle);
        if (hwnd == IntPtr.Zero)
            return false;
        
        // Verify it's from Steam process
        return IsWindowFromSteam(hwnd);
    }

    private bool IsWindowFromSteam(IntPtr hwnd)
    {
        GetWindowThreadProcessId(hwnd, out uint processId);
        try
        {
            var process = Process.GetProcessById((int)processId);
            var processName = process.ProcessName;
            return processName.Equals("steam", StringComparison.OrdinalIgnoreCase) ||
                   processName.Equals("steamwebhelper", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            // Process no longer exists or access denied
            _logger.LogDebug(ex, "Failed to check if window belongs to Steam process: {ProcessId}", processId);
            return false;
        }
    }

    private static string GetWindowClassName(IntPtr hwnd)
    {
        var className = new StringBuilder(256);
        GetClassName(hwnd, className, className.Capacity);
        return className.ToString();
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length == 0) return string.Empty;
        
        var title = new StringBuilder(length + 1);
        GetWindowText(hwnd, title, title.Capacity);
        return title.ToString();
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _retryCts?.Cancel();
        _retryCts?.Dispose();
        
        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #region P/Invoke

    private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    #endregion
}

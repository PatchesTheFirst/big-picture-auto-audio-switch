using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace BigPictureAutoAudioSwitch.Services;

public class AudioService : IAudioService, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AudioService> _logger;
    private readonly MMDeviceEnumerator _deviceEnumerator;
    private readonly PolicyConfigClient _policyConfig;
    private readonly NotificationClient _notificationClient;
    private readonly object _storedDeviceLock = new();
    private string? _storedDeviceId;
    private bool _disposed;

    public event EventHandler<AudioDevice?>? DefaultDeviceChanged;
    public event EventHandler? DevicesChanged;

    public AudioService(ISettingsService settingsService, ILogger<AudioService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        _deviceEnumerator = new MMDeviceEnumerator();
        _policyConfig = new PolicyConfigClient();
        _notificationClient = new NotificationClient(this);
    }

    public void Initialize()
    {
        // Store the initial default device
        var defaultDevice = GetDefaultDevice();
        lock (_storedDeviceLock)
        {
            _storedDeviceId = defaultDevice?.Id;
        }
        
        _logger.LogInformation("AudioService initialized. Default device: {DeviceName}", 
            defaultDevice?.FullName ?? "None");

        // Register for device notifications
        _deviceEnumerator.RegisterEndpointNotificationCallback(_notificationClient);
    }

    public IEnumerable<AudioDevice> GetPlaybackDevices()
    {
        try
        {
            var devices = new List<AudioDevice>();
            var defaultId = GetDefaultDeviceId();

            foreach (var device in _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                devices.Add(new AudioDevice(
                    Id: device.ID,
                    Name: device.DeviceFriendlyName,
                    FullName: device.FriendlyName,
                    IsDefault: device.ID == defaultId
                ));
            }

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate playback devices");
            return Enumerable.Empty<AudioDevice>();
        }
    }

    public AudioDevice? GetDefaultDevice()
    {
        try
        {
            if (!_deviceEnumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                return null;

            var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return new AudioDevice(
                Id: device.ID,
                Name: device.DeviceFriendlyName,
                FullName: device.FriendlyName,
                IsDefault: true
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default playback device");
            return null;
        }
    }

    private string? GetDefaultDeviceId()
    {
        try
        {
            if (!_deviceEnumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                return null;

            var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return device.ID;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get default device ID");
            return null;
        }
    }

    public bool DeviceExists(string deviceId)
    {
        try
        {
            var device = _deviceEnumerator.GetDevice(deviceId);
            return device != null && device.State == DeviceState.Active;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check device existence: {DeviceId}", deviceId);
            return false;
        }
    }

    public bool SetDefaultDevice(string deviceId)
    {
        try
        {
            // Verify device exists
            var device = _deviceEnumerator.GetDevice(deviceId);
            if (device == null)
            {
                _logger.LogWarning("Cannot set default device: Device with ID '{DeviceId}' was not found in the system", deviceId);
                return false;
            }

            if (device.State != DeviceState.Active)
            {
                _logger.LogWarning("Cannot set default device: Device '{DeviceName}' (ID: {DeviceId}) is in state '{State}' and not active", 
                    device.FriendlyName, deviceId, device.State);
                return false;
            }

            // Use PolicyConfig to set default device for all roles
            _policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
            _policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
            _policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications);
            
            _logger.LogInformation("Successfully set default audio device to '{DeviceName}' for all audio roles", device.FriendlyName);
            DefaultDeviceChanged?.Invoke(this, GetDefaultDevice());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default device (ID: {DeviceId}). This may be due to insufficient permissions or system restrictions.", deviceId);
            return false;
        }
    }

    public void StoreCurrentDevice()
    {
        var deviceId = GetDefaultDeviceId();
        lock (_storedDeviceLock)
        {
            _storedDeviceId = deviceId;
        }
        _logger.LogDebug("Stored current device: {DeviceId}", deviceId ?? "None");
    }

    public void RestoreStoredDevice()
    {
        string? deviceId;
        lock (_storedDeviceLock)
        {
            deviceId = _storedDeviceId;
        }
        
        if (!string.IsNullOrEmpty(deviceId))
        {
            _logger.LogInformation("Restoring previous audio device: {DeviceId}", deviceId);
            var success = SetDefaultDevice(deviceId);
            if (!success)
            {
                _logger.LogWarning("Failed to restore previous audio device. The device may have been disconnected since it was stored.");
            }
        }
        else
        {
            _logger.LogWarning("Cannot restore audio device: No device was previously stored");
        }
    }

    public async Task<bool> SwitchToTargetDeviceAsync(CancellationToken cancellationToken = default)
    {
        var targetDeviceId = _settingsService.Settings.TargetDeviceId;
        if (string.IsNullOrEmpty(targetDeviceId))
        {
            _logger.LogWarning("Cannot switch audio: No target device configured. Please configure a device in Settings.");
            return false;
        }

        _logger.LogInformation("Attempting to switch to target device: {DeviceId}", targetDeviceId);
        
        // Retry logic with exponential backoff for devices that may need time to initialize (e.g., HDMI audio)
        var maxRetries = AppConstants.MaxRetries;
        var baseDelayMs = AppConstants.RetryBaseDelayMs;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var success = SetDefaultDevice(targetDeviceId);
            if (success)
            {
                _logger.LogInformation("Successfully switched to target device on attempt {Attempt}", attempt);
                return true;
            }
            
            if (attempt < maxRetries)
            {
                // Exponential backoff: 500ms, 1000ms, 2000ms
                var delay = (int)(baseDelayMs * Math.Pow(2, attempt - 1));
                _logger.LogDebug("Device switch attempt {Attempt}/{MaxRetries} failed. Retrying in {Delay}ms (device may need initialization time)...", 
                    attempt, maxRetries, delay);
                await Task.Delay(delay, cancellationToken);
            }
        }
        
        _logger.LogWarning("Failed to switch to target device after {MaxRetries} attempts. The device may be disconnected, disabled, or unavailable. Device ID: {DeviceId}", 
            maxRetries, targetDeviceId);
        return false;
    }

    internal void OnDeviceAdded(string deviceId)
    {
        _logger.LogDebug("Device added: {DeviceId}", deviceId);
        DevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void OnDeviceRemoved(string deviceId)
    {
        _logger.LogDebug("Device removed: {DeviceId}", deviceId);
        DevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Render && role == Role.Multimedia)
        {
            _logger.LogDebug("Default device changed: {DeviceId}", defaultDeviceId);
            DefaultDeviceChanged?.Invoke(this, GetDefaultDevice());
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            _deviceEnumerator.UnregisterEndpointNotificationCallback(_notificationClient);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to unregister notification callback");
        }
        
        _deviceEnumerator.Dispose();
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }

    #region Notification Client

    private class NotificationClient : IMMNotificationClient
    {
        private readonly AudioService _audioService;

        public NotificationClient(AudioService audioService)
        {
            _audioService = audioService;
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            _audioService._logger.LogDebug("Device state changed: {DeviceId} -> {State}", deviceId, newState);
            _audioService.DevicesChanged?.Invoke(_audioService, EventArgs.Empty);
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            _audioService.OnDeviceAdded(pwstrDeviceId);
        }

        public void OnDeviceRemoved(string deviceId)
        {
            _audioService.OnDeviceRemoved(deviceId);
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            _audioService.OnDefaultDeviceChanged(flow, role, defaultDeviceId);
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
    }

    #endregion

    #region PolicyConfig COM Interop

    private enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig]
        int GetMixFormat(string pszDeviceName, IntPtr ppFormat);
        
        [PreserveSig]
        int GetDeviceFormat(string pszDeviceName, bool bDefault, IntPtr ppFormat);
        
        [PreserveSig]
        int ResetDeviceFormat(string pszDeviceName);
        
        [PreserveSig]
        int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr pMixFormat);
        
        [PreserveSig]
        int GetProcessingPeriod(string pszDeviceName, bool bDefault, IntPtr pmftDefaultPeriod, IntPtr pmftMinimumPeriod);
        
        [PreserveSig]
        int SetProcessingPeriod(string pszDeviceName, IntPtr pmftPeriod);
        
        [PreserveSig]
        int GetShareMode(string pszDeviceName, IntPtr pMode);
        
        [PreserveSig]
        int SetShareMode(string pszDeviceName, IntPtr mode);
        
        [PreserveSig]
        int GetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, IntPtr pv);
        
        [PreserveSig]
        int SetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, IntPtr pv);
        
        [PreserveSig]
        int SetDefaultEndpoint(string pszDeviceName, ERole role);
        
        [PreserveSig]
        int SetEndpointVisibility(string pszDeviceName, bool bVisible);
    }

    [ComImport]
    [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private class PolicyConfigClientClass { }

    private class PolicyConfigClient
    {
        private readonly IPolicyConfig _policyConfig;
        
        public PolicyConfigClient()
        {
            _policyConfig = (IPolicyConfig)new PolicyConfigClientClass();
        }
        
        public void SetDefaultEndpoint(string deviceId, ERole role)
        {
            _policyConfig.SetDefaultEndpoint(deviceId, role);
        }
    }

    #endregion
}

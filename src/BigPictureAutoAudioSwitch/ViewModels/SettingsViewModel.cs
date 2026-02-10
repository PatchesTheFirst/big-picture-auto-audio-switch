using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using BigPictureAutoAudioSwitch.Services;
using Application = System.Windows.Application;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace BigPictureAutoAudioSwitch.ViewModels;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IAudioService _audioService;
    private readonly IStartupService _startupService;
    private readonly ILoggingService _loggingService;
    private readonly ISettingsValidator _settingsValidator;
    private bool _disposed;

    [ObservableProperty]
    private ObservableCollection<AudioDevice> _devices = [];

    [ObservableProperty]
    private AudioDevice? _selectedDevice;

    [ObservableProperty]
    private bool _launchOnStartup;

    [ObservableProperty]
    private bool _showNotifications;

    [ObservableProperty]
    private bool _verboseLogging;

    [ObservableProperty]
    private bool _hasChanges;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _targetDeviceMissing;

    [ObservableProperty]
    private string? _validationError;

    public SettingsViewModel(
        ISettingsService settingsService, 
        IAudioService audioService, 
        IStartupService startupService,
        ILoggingService loggingService,
        ISettingsValidator settingsValidator)
    {
        _settingsService = settingsService;
        _audioService = audioService;
        _startupService = startupService;
        _loggingService = loggingService;
        _settingsValidator = settingsValidator;
        
        // Subscribe to device changes
        _audioService.DevicesChanged += OnDevicesChanged;
    }

    /// <summary>
    /// Initializes the view model asynchronously. Call this from the view's Loaded event.
    /// </summary>
    public async Task InitializeAsync()
    {
        IsLoading = true;
        
        try
        {
            await LoadSettingsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnDevicesChanged(object? sender, EventArgs e)
    {
        // Refresh devices on UI thread
        Application.Current?.Dispatcher.Invoke(RefreshDevices);
    }

    private async Task LoadSettingsAsync()
    {
        // Load devices
        Devices.Clear();
        foreach (var device in _audioService.GetPlaybackDevices())
        {
            Devices.Add(device);
        }

        // Set selected device and check if it still exists
        var targetId = _settingsService.Settings.TargetDeviceId;
        TargetDeviceMissing = false;
        
        if (!string.IsNullOrEmpty(targetId))
        {
            SelectedDevice = Devices.FirstOrDefault(d => d.Id == targetId);
            
            // Check if the saved device no longer exists
            if (SelectedDevice == null && !_audioService.DeviceExists(targetId))
            {
                TargetDeviceMissing = true;
            }
        }

        // Load other settings
        // Use IsEnabledAndValidAsync to ensure the checkbox reflects reality
        // (will be false if app was moved after enabling startup)
        LaunchOnStartup = await _startupService.IsEnabledAndValidAsync();
        ShowNotifications = _settingsService.Settings.ShowNotifications;
        VerboseLogging = _loggingService.IsVerboseLogging;
        
        HasChanges = false;
    }

    partial void OnSelectedDeviceChanged(AudioDevice? value)
    {
        if (!IsLoading)
        {
            HasChanges = true;
        }
    }

    partial void OnLaunchOnStartupChanged(bool value)
    {
        if (!IsLoading)
        {
            HasChanges = true;
        }
    }

    partial void OnShowNotificationsChanged(bool value)
    {
        if (!IsLoading)
        {
            HasChanges = true;
        }
    }

    partial void OnVerboseLoggingChanged(bool value)
    {
        if (!IsLoading)
        {
            HasChanges = true;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Validate settings before saving
        ValidationError = null;
        var validation = _settingsValidator.ValidateTargetDevice(SelectedDevice?.Id);
        if (!validation.IsValid)
        {
            ValidationError = validation.ErrorMessage;
            MessageBox.Show(
                validation.ErrorMessage ?? "Validation failed",
                "Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _settingsService.Settings.TargetDeviceId = SelectedDevice?.Id;
        _settingsService.Settings.ShowNotifications = ShowNotifications;
        
        // Apply verbose logging setting (this updates the level switch immediately)
        _loggingService.SetVerboseLogging(VerboseLogging);
        
        await _settingsService.SaveAsync();
        await _startupService.SetEnabledAsync(LaunchOnStartup);
        
        HasChanges = false;
        TargetDeviceMissing = false;
    }

    [RelayCommand]
    private void RefreshDevices()
    {
        var currentId = SelectedDevice?.Id;
        
        Devices.Clear();
        foreach (var device in _audioService.GetPlaybackDevices())
        {
            Devices.Add(device);
        }

        // Try to restore selection
        if (!string.IsNullOrEmpty(currentId))
        {
            SelectedDevice = Devices.FirstOrDefault(d => d.Id == currentId);
        }
    }

    [RelayCommand]
    private void TestDevice()
    {
        if (SelectedDevice != null)
        {
            _audioService.SetDefaultDevice(SelectedDevice.Id);
            MessageBox.Show(
                $"Audio switched to: {SelectedDevice.FullName}\n\nThis is a test - the device will remain active until changed.",
                "Test Device",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        var folder = _loggingService.GetLogsFolder();
        if (Directory.Exists(folder))
        {
            Process.Start("explorer.exe", folder);
        }
        else
        {
            MessageBox.Show(
                "The logs folder does not exist yet. Logs will be created when the application runs.",
                "Logs Folder",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _audioService.DevicesChanged -= OnDevicesChanged;
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }
}

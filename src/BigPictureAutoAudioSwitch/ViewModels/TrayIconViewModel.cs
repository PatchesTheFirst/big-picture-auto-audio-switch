using System.Diagnostics;
using BigPictureAutoAudioSwitch.Services;
using BigPictureAutoAudioSwitch.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Application = System.Windows.Application;

namespace BigPictureAutoAudioSwitch.ViewModels;

public partial class TrayIconViewModel : ObservableObject, IDisposable
{
    private readonly IBigPictureDetector _detector;
    private readonly IUpdateCheckService _updateCheckService;
    private SettingsWindow? _settingsWindow;
    private AboutWindow? _aboutWindow;
    private bool _disposed;

    [ObservableProperty]
    private string _statusText = "Monitoring for Big Picture";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdateAvailable))]
    [NotifyPropertyChangedFor(nameof(UpdateMenuHeader))]
    private UpdateInfo? _availableUpdate;

    public bool UpdateAvailable => AvailableUpdate != null;

    public string UpdateMenuHeader => AvailableUpdate != null
        ? $"Update available (v{AvailableUpdate.Version})..."
        : string.Empty;

    public TrayIconViewModel(IBigPictureDetector detector, IUpdateCheckService updateCheckService)
    {
        _detector = detector;
        _updateCheckService = updateCheckService;
        _detector.BigPictureStateChanged += OnBigPictureStateChanged;
        _updateCheckService.UpdateAvailable += OnUpdateAvailable;
        AvailableUpdate = _updateCheckService.AvailableUpdate;
        UpdateStatus();
    }

    private void OnBigPictureStateChanged(object? sender, bool isActive)
    {
        UpdateStatus();
    }

    private void OnUpdateAvailable(object? sender, UpdateInfo info)
    {
        // Update checks run on a background thread; marshal to the UI thread for bindings
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null)
        {
            dispatcher.Invoke(() => AvailableUpdate = info);
        }
        else
        {
            AvailableUpdate = info;
        }
    }

    private void UpdateStatus()
    {
        StatusText = _detector.IsBigPictureActive
            ? "Big Picture Mode Active"
            : "Monitoring for Big Picture";
    }

    [RelayCommand]
    private void ShowSettings()
    {
        if (_settingsWindow != null && _settingsWindow.IsVisible)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = App.Services.GetRequiredService<SettingsWindow>();
        _settingsWindow.Closed += (s, e) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    [RelayCommand]
    private void ShowAbout()
    {
        if (_aboutWindow != null && _aboutWindow.IsVisible)
        {
            _aboutWindow.Activate();
            return;
        }

        _aboutWindow = App.Services.GetRequiredService<AboutWindow>();
        _aboutWindow.Closed += (s, e) => _aboutWindow = null;
        _aboutWindow.Show();
    }

    [RelayCommand]
    private void OpenReleasePage()
    {
        var url = AvailableUpdate?.ReleaseUrl;
        if (string.IsNullOrEmpty(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently fail if browser can't be opened
        }
    }

    [RelayCommand]
    private void Exit()
    {
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _detector.BigPictureStateChanged -= OnBigPictureStateChanged;
        _updateCheckService.UpdateAvailable -= OnUpdateAvailable;
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

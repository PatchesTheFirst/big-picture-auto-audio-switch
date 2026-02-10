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
    private SettingsWindow? _settingsWindow;
    private AboutWindow? _aboutWindow;
    private bool _disposed;

    [ObservableProperty]
    private string _statusText = "Monitoring for Big Picture";

    public TrayIconViewModel(IBigPictureDetector detector)
    {
        _detector = detector;
        _detector.BigPictureStateChanged += OnBigPictureStateChanged;
        UpdateStatus();
    }

    private void OnBigPictureStateChanged(object? sender, bool isActive)
    {
        UpdateStatus();
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
    private void Exit()
    {
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _detector.BigPictureStateChanged -= OnBigPictureStateChanged;
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }
}

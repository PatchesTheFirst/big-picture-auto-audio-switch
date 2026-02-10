using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BigPictureAutoAudioSwitch.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public string AppName => "Big Picture Auto Audio Switch";
    
    public string Version
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"Version {version.Major}.{version.Minor}.{version.Build}" : "Version Unknown";
        }
    }
    
    public string Description => "Automatically switches your audio output device when Steam Big Picture Mode is detected.";
    
    public string Copyright => $"© {DateTime.Now.Year} Big Picture Auto Audio Switch";
    
    public string GitHubUrl => "https://github.com/yourusername/big-picture-auto-audio-switch";

    [RelayCommand]
    private void OpenGitHub()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GitHubUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently fail if browser can't be opened
        }
    }
}

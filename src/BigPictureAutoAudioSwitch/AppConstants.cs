using System.IO;

namespace BigPictureAutoAudioSwitch;

public static class AppConstants
{
    public const string AppName = "BigPictureAutoAudioSwitch";
    public const string MutexName = "BigPictureAutoAudioSwitch_SingleInstance";
    public const string ShowSettingsEventName = "BigPictureAutoAudioSwitch_ShowSettings";
    
    // Paths
    public static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppName);
    public static readonly string LogsFolder = Path.Combine(AppDataFolder, "logs");
    public static readonly string SettingsFile = Path.Combine(AppDataFolder, "settings.json");
    
    // Retry logic
    public const int RetryBaseDelayMs = 500;
    public const int MaxRetries = 3;
    public const int BackgroundRetryDelayMs = 5000;
    public const int BackgroundRetryMaxAttempts = 6;
    
    // Debounce
    public static readonly TimeSpan DeactivationCooldown = TimeSpan.FromMilliseconds(1000);
    
    // Logging
    public static readonly TimeSpan VerboseLoggingTimeout = TimeSpan.FromHours(48);
    public const int LogRetainedFileCount = 7;
    public const long LogFileSizeLimitBytes = 50_000_000;

    // Update checks
    public const string GitHubRepoUrl = "https://github.com/PatchesTheFirst/big-picture-auto-audio-switch";
    public const string LatestReleaseApiUrl = "https://api.github.com/repos/PatchesTheFirst/big-picture-auto-audio-switch/releases/latest";
    public static readonly TimeSpan UpdateCheckInitialDelay = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(24);
}

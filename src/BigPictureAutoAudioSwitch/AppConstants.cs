using System.IO;

namespace BigPictureAutoAudioSwitch;

public static class AppConstants
{
    public const string AppName = "BigPictureAutoAudioSwitch";
    public const string MutexName = "BigPictureAutoAudioSwitch_SingleInstance";
    
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
}

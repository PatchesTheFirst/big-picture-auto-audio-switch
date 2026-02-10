namespace BigPictureAutoAudioSwitch.Services;

public class AppSettings
{
    public string? TargetDeviceId { get; set; }
    public bool LaunchOnStartup { get; set; }
    public bool ShowNotifications { get; set; } = true;
    public bool VerboseLogging { get; set; } = false;
    public DateTime? VerboseLoggingEnabledAt { get; set; }
}

public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    AppSettings Settings { get; }

    /// <summary>
    /// Loads settings from disk.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when settings are changed.
    /// </summary>
    event EventHandler? SettingsChanged;
}

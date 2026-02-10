namespace BigPictureAutoAudioSwitch.Services;

/// <summary>
/// Service for managing application logging configuration.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Gets whether verbose (debug) logging is currently enabled.
    /// </summary>
    bool IsVerboseLogging { get; }

    /// <summary>
    /// Sets whether verbose logging is enabled and updates the log level accordingly.
    /// </summary>
    /// <param name="enabled">True to enable verbose logging, false for normal logging.</param>
    void SetVerboseLogging(bool enabled);

    /// <summary>
    /// Gets the folder path where log files are stored.
    /// </summary>
    string GetLogsFolder();

    /// <summary>
    /// Checks if verbose logging has been enabled for more than 48 hours and auto-disables it if so.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    Task CheckAutoDisableAsync(CancellationToken cancellationToken = default);
}

namespace BigPictureAutoAudioSwitch.Services;

/// <summary>
/// Information about an available application update.
/// </summary>
/// <param name="Version">The version of the latest release (e.g. "1.0.2").</param>
/// <param name="ReleaseUrl">The URL of the release page to download from.</param>
public record UpdateInfo(string Version, string ReleaseUrl);

public interface IUpdateCheckService
{
    /// <summary>
    /// Gets the update found by the most recent check, or null if the app is up to date.
    /// </summary>
    UpdateInfo? AvailableUpdate { get; }

    /// <summary>
    /// Event raised when a newer release is detected.
    /// May be raised on a background thread.
    /// </summary>
    event EventHandler<UpdateInfo>? UpdateAvailable;

    /// <summary>
    /// Starts periodic background update checks (respects the CheckForUpdates setting).
    /// </summary>
    void Start();

    /// <summary>
    /// Stops periodic background update checks.
    /// </summary>
    void Stop();

    /// <summary>
    /// Checks GitHub for a newer release once.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update info if a newer release exists, otherwise null.</returns>
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);
}

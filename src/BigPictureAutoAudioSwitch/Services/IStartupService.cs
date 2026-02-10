namespace BigPictureAutoAudioSwitch.Services;

public interface IStartupService
{
    /// <summary>
    /// Gets whether the app is configured to launch on startup.
    /// </summary>
    Task<bool> IsEnabledAsync();

    /// <summary>
    /// Gets whether startup is enabled AND the registered path matches the current executable.
    /// Returns false if the app was moved after enabling startup.
    /// </summary>
    Task<bool> IsEnabledAndValidAsync();

    /// <summary>
    /// Enables or disables launch on startup.
    /// </summary>
    Task SetEnabledAsync(bool enabled);
}

namespace BigPictureAutoAudioSwitch.Services;

public interface INotificationService
{
    /// <summary>
    /// Shows a notification that audio has been switched.
    /// </summary>
    /// <param name="deviceName">The name of the device switched to.</param>
    /// <param name="isBigPictureMode">True if switching to Big Picture mode, false if restoring.</param>
    void ShowAudioSwitched(string deviceName, bool isBigPictureMode);

    /// <summary>
    /// Shows a notification that the configured audio device is missing.
    /// </summary>
    /// <param name="deviceName">The name of the missing device.</param>
    void ShowDeviceMissing(string deviceName);

    /// <summary>
    /// Shows a notification that a newer application version is available.
    /// Clicking the notification opens the release page.
    /// </summary>
    /// <param name="version">The version of the new release (e.g. "1.0.2").</param>
    /// <param name="releaseUrl">The URL of the release page.</param>
    void ShowUpdateAvailable(string version, string releaseUrl);
}

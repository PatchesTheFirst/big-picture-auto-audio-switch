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
}

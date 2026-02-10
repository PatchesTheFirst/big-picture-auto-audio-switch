namespace BigPictureAutoAudioSwitch.Services;

public record AudioDevice(string Id, string Name, string FullName, bool IsDefault);

public interface IAudioService
{
    /// <summary>
    /// Initialize the audio service.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Gets all available playback devices.
    /// </summary>
    IEnumerable<AudioDevice> GetPlaybackDevices();

    /// <summary>
    /// Gets the current default playback device.
    /// </summary>
    AudioDevice? GetDefaultDevice();

    /// <summary>
    /// Sets the default playback device.
    /// </summary>
    /// <param name="deviceId">The device ID to set as default.</param>
    /// <returns>True if successful.</returns>
    bool SetDefaultDevice(string deviceId);

    /// <summary>
    /// Stores the current default device for later restoration.
    /// </summary>
    void StoreCurrentDevice();

    /// <summary>
    /// Restores the previously stored device as default.
    /// </summary>
    void RestoreStoredDevice();

    /// <summary>
    /// Switches to the target device specified in settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>True if successful, false if device is missing or not configured.</returns>
    Task<bool> SwitchToTargetDeviceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a device with the specified ID exists and is available.
    /// </summary>
    /// <param name="deviceId">The device ID to check.</param>
    /// <returns>True if the device exists and is active.</returns>
    bool DeviceExists(string deviceId);

    /// <summary>
    /// Event raised when the default device changes.
    /// </summary>
    event EventHandler<AudioDevice?>? DefaultDeviceChanged;

    /// <summary>
    /// Event raised when devices are added or removed.
    /// </summary>
    event EventHandler? DevicesChanged;
}

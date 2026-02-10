namespace BigPictureAutoAudioSwitch.Services;

public interface IBigPictureDetector
{
    /// <summary>
    /// Gets whether Big Picture mode is currently active.
    /// </summary>
    bool IsBigPictureActive { get; }

    /// <summary>
    /// Start monitoring for Big Picture mode.
    /// </summary>
    void Start();

    /// <summary>
    /// Stop monitoring for Big Picture mode.
    /// </summary>
    void Stop();

    /// <summary>
    /// Event raised when Big Picture mode state changes.
    /// </summary>
    event EventHandler<bool>? BigPictureStateChanged;
}

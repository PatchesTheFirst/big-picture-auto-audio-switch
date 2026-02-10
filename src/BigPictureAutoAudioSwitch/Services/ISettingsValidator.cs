namespace BigPictureAutoAudioSwitch.Services;

/// <summary>
/// Validation result containing success status and optional error message.
/// </summary>
/// <param name="IsValid">Whether the validation passed.</param>
/// <param name="ErrorMessage">Error message if validation failed, null otherwise.</param>
public record ValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static ValidationResult Success => new(true);
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// Service for validating application settings before saving.
/// </summary>
public interface ISettingsValidator
{
    /// <summary>
    /// Validates the target device configuration.
    /// </summary>
    /// <param name="deviceId">The device ID to validate, or null if no device is selected.</param>
    /// <returns>Validation result indicating success or failure with error message.</returns>
    ValidationResult ValidateTargetDevice(string? deviceId);

    /// <summary>
    /// Validates all settings before saving.
    /// </summary>
    /// <param name="settings">The settings to validate.</param>
    /// <returns>Validation result indicating success or failure with error message.</returns>
    ValidationResult ValidateSettings(AppSettings settings);
}

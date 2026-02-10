using Microsoft.Extensions.Logging;

namespace BigPictureAutoAudioSwitch.Services;

/// <summary>
/// Validates application settings before saving.
/// </summary>
public class SettingsValidator : ISettingsValidator
{
    private readonly IAudioService _audioService;
    private readonly ILogger<SettingsValidator> _logger;

    public SettingsValidator(IAudioService audioService, ILogger<SettingsValidator> logger)
    {
        _audioService = audioService;
        _logger = logger;
    }

    public ValidationResult ValidateTargetDevice(string? deviceId)
    {
        // No device configured is valid (user may want to disable switching)
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            _logger.LogDebug("Target device validation: No device configured (valid)");
            return ValidationResult.Success;
        }

        // Check if the device exists and is active
        if (!_audioService.DeviceExists(deviceId))
        {
            _logger.LogWarning("Target device validation failed: Device '{DeviceId}' is not available", deviceId);
            return ValidationResult.Failure(
                "The selected audio device is no longer available. Please select a different device or refresh the device list.");
        }

        _logger.LogDebug("Target device validation passed for device '{DeviceId}'", deviceId);
        return ValidationResult.Success;
    }

    public ValidationResult ValidateSettings(AppSettings settings)
    {
        // Validate target device
        var deviceValidation = ValidateTargetDevice(settings.TargetDeviceId);
        if (!deviceValidation.IsValid)
        {
            return deviceValidation;
        }

        // Validate verbose logging timestamp consistency
        if (settings.VerboseLogging && settings.VerboseLoggingEnabledAt == null)
        {
            _logger.LogDebug("Settings validation: Verbose logging enabled without timestamp, will set timestamp on save");
            // This is a correctable condition, not an error
        }

        if (!settings.VerboseLogging && settings.VerboseLoggingEnabledAt != null)
        {
            _logger.LogDebug("Settings validation: Verbose logging disabled but timestamp present, will clear on save");
            // This is a correctable condition, not an error
        }

        return ValidationResult.Success;
    }
}

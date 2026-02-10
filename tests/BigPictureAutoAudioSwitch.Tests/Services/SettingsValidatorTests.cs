using BigPictureAutoAudioSwitch.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BigPictureAutoAudioSwitch.Tests.Services;

public class SettingsValidatorTests
{
    private readonly Mock<IAudioService> _audioServiceMock;
    private readonly Mock<ILogger<SettingsValidator>> _loggerMock;
    private readonly SettingsValidator _validator;

    public SettingsValidatorTests()
    {
        _audioServiceMock = new Mock<IAudioService>();
        _loggerMock = new Mock<ILogger<SettingsValidator>>();
        _validator = new SettingsValidator(_audioServiceMock.Object, _loggerMock.Object);
    }

    #region ValidateTargetDevice Tests

    [Fact]
    public void ValidateTargetDevice_WithNullDeviceId_ReturnsSuccess()
    {
        // Act
        var result = _validator.ValidateTargetDevice(null);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateTargetDevice_WithEmptyDeviceId_ReturnsSuccess()
    {
        // Act
        var result = _validator.ValidateTargetDevice("");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateTargetDevice_WithWhitespaceDeviceId_ReturnsSuccess()
    {
        // Act
        var result = _validator.ValidateTargetDevice("   ");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateTargetDevice_WithExistingDevice_ReturnsSuccess()
    {
        // Arrange
        var deviceId = "test-device-123";
        _audioServiceMock.Setup(x => x.DeviceExists(deviceId)).Returns(true);

        // Act
        var result = _validator.ValidateTargetDevice(deviceId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateTargetDevice_WithNonExistingDevice_ReturnsFailure()
    {
        // Arrange
        var deviceId = "missing-device-456";
        _audioServiceMock.Setup(x => x.DeviceExists(deviceId)).Returns(false);

        // Act
        var result = _validator.ValidateTargetDevice(deviceId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("no longer available");
    }

    #endregion

    #region ValidateSettings Tests

    [Fact]
    public void ValidateSettings_WithNoTargetDevice_ReturnsSuccess()
    {
        // Arrange
        var settings = new AppSettings { TargetDeviceId = null };

        // Act
        var result = _validator.ValidateSettings(settings);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateSettings_WithValidTargetDevice_ReturnsSuccess()
    {
        // Arrange
        var deviceId = "valid-device";
        _audioServiceMock.Setup(x => x.DeviceExists(deviceId)).Returns(true);
        var settings = new AppSettings { TargetDeviceId = deviceId };

        // Act
        var result = _validator.ValidateSettings(settings);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateSettings_WithInvalidTargetDevice_ReturnsFailure()
    {
        // Arrange
        var deviceId = "invalid-device";
        _audioServiceMock.Setup(x => x.DeviceExists(deviceId)).Returns(false);
        var settings = new AppSettings { TargetDeviceId = deviceId };

        // Act
        var result = _validator.ValidateSettings(settings);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateSettings_WithVerboseLoggingEnabled_ReturnsSuccess()
    {
        // Arrange - verbose logging enabled without timestamp (correctable, not an error)
        var settings = new AppSettings
        {
            TargetDeviceId = null,
            VerboseLogging = true,
            VerboseLoggingEnabledAt = null
        };

        // Act
        var result = _validator.ValidateSettings(settings);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateSettings_WithVerboseLoggingDisabledButTimestampPresent_ReturnsSuccess()
    {
        // Arrange - verbose logging disabled but timestamp present (correctable, not an error)
        var settings = new AppSettings
        {
            TargetDeviceId = null,
            VerboseLogging = false,
            VerboseLoggingEnabledAt = DateTime.UtcNow
        };

        // Act
        var result = _validator.ValidateSettings(settings);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ValidationResult Tests

    [Fact]
    public void ValidationResult_Success_HasCorrectProperties()
    {
        // Act
        var result = ValidationResult.Success;

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidationResult_Failure_HasCorrectProperties()
    {
        // Arrange
        var errorMessage = "Test error message";

        // Act
        var result = ValidationResult.Failure(errorMessage);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
    }

    #endregion
}

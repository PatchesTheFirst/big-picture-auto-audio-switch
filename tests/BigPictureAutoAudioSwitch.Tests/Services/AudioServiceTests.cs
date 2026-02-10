using BigPictureAutoAudioSwitch.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BigPictureAutoAudioSwitch.Tests.Services;

public class AudioServiceTests : IDisposable
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<AudioService>> _loggerMock;
    private AudioService? _audioService;

    public AudioServiceTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _loggerMock = new Mock<ILogger<AudioService>>();
        _settingsServiceMock.Setup(s => s.Settings).Returns(new AppSettings());
    }

    public void Dispose()
    {
        _audioService?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        // Act
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);

        // Assert
        _audioService.Should().NotBeNull();
    }

    [Fact]
    public void Initialize_DoesNotThrow()
    {
        // Arrange
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);

        // Act & Assert - Should not throw
        _audioService.Initialize();
    }

    [Fact]
    public void GetPlaybackDevices_ReturnsCollection()
    {
        // Arrange
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        _audioService.Initialize();

        // Act
        var devices = _audioService.GetPlaybackDevices();

        // Assert
        devices.Should().NotBeNull();
        // Note: The actual count depends on the test machine's audio devices
    }

    [Fact]
    public void GetDefaultDevice_ReturnsDeviceOrNull()
    {
        // Arrange
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        _audioService.Initialize();

        // Act
        var device = _audioService.GetDefaultDevice();

        // Assert - Can be null if no audio devices are present
        // Just verify it doesn't throw
        if (device != null)
        {
            device.Id.Should().NotBeNullOrEmpty();
            device.Name.Should().NotBeNull();
            device.FullName.Should().NotBeNull();
        }
    }

    [Fact]
    public void DeviceExists_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        _audioService.Initialize();

        // Act
        var exists = _audioService.DeviceExists("non-existent-device-id-12345");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public void DeviceExists_WithEmptyId_ReturnsFalse()
    {
        // Arrange
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        _audioService.Initialize();

        // Act
        var exists = _audioService.DeviceExists("");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public void SetDefaultDevice_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        _audioService.Initialize();

        // Act
        var result = _audioService.SetDefaultDevice("non-existent-device-id-12345");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void StoreCurrentDevice_DoesNotThrow()
    {
        // Arrange
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        _audioService.Initialize();

        // Act & Assert - Should not throw
        _audioService.StoreCurrentDevice();
    }

    [Fact]
    public void RestoreStoredDevice_WithNoStoredDevice_DoesNotThrow()
    {
        // Arrange
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        // Don't call Initialize or StoreCurrentDevice - storedDeviceId should be null

        // Act & Assert - Should not throw
        _audioService.RestoreStoredDevice();
    }

    [Fact]
    public async Task SwitchToTargetDeviceAsync_WithNoTargetConfigured_ReturnsFalse()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.Settings).Returns(new AppSettings { TargetDeviceId = null });
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        _audioService.Initialize();

        // Act
        var result = await _audioService.SwitchToTargetDeviceAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SwitchToTargetDeviceAsync_WithEmptyTarget_ReturnsFalse()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.Settings).Returns(new AppSettings { TargetDeviceId = "" });
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        _audioService.Initialize();

        // Act
        var result = await _audioService.SwitchToTargetDeviceAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SwitchToTargetDeviceAsync_WithInvalidTarget_ReturnsFalse()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.Settings).Returns(new AppSettings { TargetDeviceId = "invalid-device-id" });
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        _audioService.Initialize();

        // Act
        var result = await _audioService.SwitchToTargetDeviceAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        _audioService.Initialize();

        // Act & Assert - Should not throw
        _audioService.Dispose();
        _audioService.Dispose();
        
        // Clear reference so Dispose in test cleanup doesn't try again
        _audioService = null;
    }

    [Fact]
    public void DefaultDeviceChanged_EventCanBeSubscribed()
    {
        // Arrange
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        EventHandler<AudioDevice?>? handler = null;

        // Act - Should not throw
        handler = (s, device) => { };
        _audioService.DefaultDeviceChanged += handler;
        _audioService.DefaultDeviceChanged -= handler;

        // Assert
        _audioService.Should().NotBeNull();
    }

    [Fact]
    public void DevicesChanged_EventCanBeSubscribed()
    {
        // Arrange
        _audioService = new AudioService(_settingsServiceMock.Object, _loggerMock.Object);
        EventHandler? handler = null;

        // Act - Should not throw
        handler = (s, e) => { };
        _audioService.DevicesChanged += handler;
        _audioService.DevicesChanged -= handler;

        // Assert
        _audioService.Should().NotBeNull();
    }
}

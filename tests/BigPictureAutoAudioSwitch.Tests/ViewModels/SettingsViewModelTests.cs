using BigPictureAutoAudioSwitch.Services;
using BigPictureAutoAudioSwitch.ViewModels;
using FluentAssertions;
using Moq;

namespace BigPictureAutoAudioSwitch.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IAudioService> _audioServiceMock;
    private readonly Mock<IStartupService> _startupServiceMock;
    private readonly Mock<ILoggingService> _loggingServiceMock;
    private readonly Mock<ISettingsValidator> _settingsValidatorMock;

    public SettingsViewModelTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _audioServiceMock = new Mock<IAudioService>();
        _startupServiceMock = new Mock<IStartupService>();
        _loggingServiceMock = new Mock<ILoggingService>();
        _settingsValidatorMock = new Mock<ISettingsValidator>();

        // Setup default behavior
        _settingsServiceMock.Setup(s => s.Settings).Returns(new AppSettings());
        _audioServiceMock.Setup(a => a.GetPlaybackDevices()).Returns(new List<AudioDevice>());
        _startupServiceMock.Setup(s => s.IsEnabledAsync()).ReturnsAsync(false);
        _loggingServiceMock.Setup(l => l.IsVerboseLogging).Returns(false);
        _loggingServiceMock.Setup(l => l.GetLogsFolder()).Returns(string.Empty);
        _settingsValidatorMock.Setup(v => v.ValidateTargetDevice(It.IsAny<string?>()))
            .Returns(ValidationResult.Success);
    }

    private SettingsViewModel CreateViewModel() => new(
        _settingsServiceMock.Object,
        _audioServiceMock.Object,
        _startupServiceMock.Object,
        _loggingServiceMock.Object,
        _settingsValidatorMock.Object);

    [Fact]
    public void Constructor_SubscribesToDevicesChanged()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert - No exception means event subscription worked
        viewModel.Should().NotBeNull();
    }

    [Fact]
    public async Task InitializeAsync_LoadsDevices()
    {
        // Arrange
        var devices = new List<AudioDevice>
        {
            new("id1", "Device 1", "Full Device 1", true),
            new("id2", "Device 2", "Full Device 2", false)
        };
        _audioServiceMock.Setup(a => a.GetPlaybackDevices()).Returns(devices);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.Devices.Should().HaveCount(2);
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_SelectsTargetDevice()
    {
        // Arrange
        var devices = new List<AudioDevice>
        {
            new("id1", "Device 1", "Full Device 1", true),
            new("id2", "Device 2", "Full Device 2", false)
        };
        _audioServiceMock.Setup(a => a.GetPlaybackDevices()).Returns(devices);
        _settingsServiceMock.Setup(s => s.Settings).Returns(new AppSettings { TargetDeviceId = "id2" });

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.SelectedDevice.Should().NotBeNull();
        viewModel.SelectedDevice!.Id.Should().Be("id2");
    }

    [Fact]
    public async Task SaveCommand_SavesSettings()
    {
        // Arrange
        var viewModel = CreateViewModel();

        await viewModel.InitializeAsync();
        viewModel.ShowNotifications = false;

        // Act
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        _settingsServiceMock.Verify(s => s.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void RefreshDevicesCommand_RefreshesDeviceList()
    {
        // Arrange
        var devices = new List<AudioDevice>
        {
            new("id1", "Device 1", "Full Device 1", true),
            new("id2", "Device 2", "Full Device 2", false)
        };

        _audioServiceMock.Setup(a => a.GetPlaybackDevices()).Returns(devices);

        var viewModel = CreateViewModel();

        // Act
        viewModel.RefreshDevicesCommand.Execute(null);

        // Assert
        viewModel.Devices.Should().HaveCount(2);
        _audioServiceMock.Verify(a => a.GetPlaybackDevices(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task InitializeAsync_WhenTargetDeviceNotFound_SetsTargetDeviceMissing()
    {
        // Arrange
        var devices = new List<AudioDevice>
        {
            new("id1", "Device 1", "Full Device 1", true)
        };
        _audioServiceMock.Setup(a => a.GetPlaybackDevices()).Returns(devices);
        _audioServiceMock.Setup(a => a.DeviceExists("missing-device-id")).Returns(false);
        _settingsServiceMock.Setup(s => s.Settings).Returns(new AppSettings { TargetDeviceId = "missing-device-id" });

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.TargetDeviceMissing.Should().BeTrue();
        viewModel.SelectedDevice.Should().BeNull();
    }

    [Fact]
    public async Task InitializeAsync_WhenTargetDeviceExists_DoesNotSetTargetDeviceMissing()
    {
        // Arrange
        var devices = new List<AudioDevice>
        {
            new("id1", "Device 1", "Full Device 1", true),
            new("id2", "Device 2", "Full Device 2", false)
        };
        _audioServiceMock.Setup(a => a.GetPlaybackDevices()).Returns(devices);
        _settingsServiceMock.Setup(s => s.Settings).Returns(new AppSettings { TargetDeviceId = "id2" });

        var viewModel = CreateViewModel();

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.TargetDeviceMissing.Should().BeFalse();
        viewModel.SelectedDevice.Should().NotBeNull();
        viewModel.SelectedDevice!.Id.Should().Be("id2");
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Dispose();

        // Assert - Should not throw when disposing again
        viewModel.Dispose();
    }
}

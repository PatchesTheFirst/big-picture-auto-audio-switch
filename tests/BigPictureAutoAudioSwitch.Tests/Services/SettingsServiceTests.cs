using System.IO;
using System.Text.Json;
using BigPictureAutoAudioSwitch.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BigPictureAutoAudioSwitch.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _testFolder;
    private readonly Mock<ILogger<SettingsService>> _loggerMock;

    public SettingsServiceTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), $"BigPictureTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFolder);
        _loggerMock = new Mock<ILogger<SettingsService>>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
        {
            Directory.Delete(_testFolder, true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task LoadAsync_WhenNoSettingsFile_UsesDefaults()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);

        // Act
        await service.LoadAsync();

        // Assert
        service.Settings.Should().NotBeNull();
        service.Settings.TargetDeviceId.Should().BeNull();
        service.Settings.ShowNotifications.Should().BeTrue();
        service.Settings.LaunchOnStartup.Should().BeFalse();
    }

    [Fact]
    public void Settings_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        settings.TargetDeviceId.Should().BeNull();
        settings.ShowNotifications.Should().BeTrue();
        settings.LaunchOnStartup.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_RaisesSettingsChangedEvent()
    {
        // Arrange
        var service = new SettingsService(_loggerMock.Object);
        var eventRaised = false;
        service.SettingsChanged += (s, e) => eventRaised = true;

        // Act
        await service.SaveAsync();

        // Assert
        eventRaised.Should().BeTrue();
    }
}

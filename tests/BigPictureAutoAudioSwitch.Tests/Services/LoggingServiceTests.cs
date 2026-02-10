using System.IO;
using BigPictureAutoAudioSwitch.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog.Core;
using Serilog.Events;

namespace BigPictureAutoAudioSwitch.Tests.Services;

public class LoggingServiceTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<LoggingService>> _loggerMock;
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly AppSettings _settings;

    public LoggingServiceTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _loggerMock = new Mock<ILogger<LoggingService>>();
        _levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        _settings = new AppSettings();

        _settingsServiceMock.Setup(s => s.Settings).Returns(_settings);
    }

    [Fact]
    public void IsVerboseLogging_ReturnsSettingsValue()
    {
        // Arrange
        _settings.VerboseLogging = true;
        var service = CreateService();

        // Act & Assert
        service.IsVerboseLogging.Should().BeTrue();

        _settings.VerboseLogging = false;
        service.IsVerboseLogging.Should().BeFalse();
    }

    [Fact]
    public void GetLogsFolder_ReturnsExpectedPath()
    {
        // Arrange
        var service = CreateService();
        var expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BigPictureAutoAudioSwitch",
            "logs");

        // Act
        var result = service.GetLogsFolder();

        // Assert
        result.Should().Be(expectedPath);
    }

    [Fact]
    public void SetVerboseLogging_WhenEnabled_SetsDebugLevelAndTimestamp()
    {
        // Arrange
        var service = CreateService();
        _settings.VerboseLogging = false;
        _settings.VerboseLoggingEnabledAt = null;

        // Act
        service.SetVerboseLogging(true);

        // Assert
        _settings.VerboseLogging.Should().BeTrue();
        _settings.VerboseLoggingEnabledAt.Should().NotBeNull();
        _settings.VerboseLoggingEnabledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Debug);
    }

    [Fact]
    public void SetVerboseLogging_WhenDisabled_SetsInformationLevelAndClearsTimestamp()
    {
        // Arrange
        var service = CreateService();
        _settings.VerboseLogging = true;
        _settings.VerboseLoggingEnabledAt = DateTime.UtcNow.AddHours(-1);
        _levelSwitch.MinimumLevel = LogEventLevel.Debug;

        // Act
        service.SetVerboseLogging(false);

        // Assert
        _settings.VerboseLogging.Should().BeFalse();
        _settings.VerboseLoggingEnabledAt.Should().BeNull();
        _levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public async Task CheckAutoDisableAsync_WhenVerboseDisabled_KeepsInformationLevel()
    {
        // Arrange
        var service = CreateService();
        _settings.VerboseLogging = false;
        _settings.VerboseLoggingEnabledAt = null;

        // Act
        await service.CheckAutoDisableAsync();

        // Assert
        _levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Information);
        _settingsServiceMock.Verify(s => s.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckAutoDisableAsync_WhenVerboseEnabledWithinTimeout_KeepsDebugLevel()
    {
        // Arrange
        var service = CreateService();
        _settings.VerboseLogging = true;
        _settings.VerboseLoggingEnabledAt = DateTime.UtcNow.AddHours(-1); // 1 hour ago

        // Act
        await service.CheckAutoDisableAsync();

        // Assert
        _levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Debug);
        _settings.VerboseLogging.Should().BeTrue();
        _settingsServiceMock.Verify(s => s.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckAutoDisableAsync_WhenVerboseEnabledPastTimeout_AutoDisablesAndSaves()
    {
        // Arrange
        var service = CreateService();
        _settings.VerboseLogging = true;
        _settings.VerboseLoggingEnabledAt = DateTime.UtcNow.AddHours(-49); // 49 hours ago (past 48h timeout)

        // Act
        await service.CheckAutoDisableAsync();

        // Assert
        _levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Information);
        _settings.VerboseLogging.Should().BeFalse();
        _settings.VerboseLoggingEnabledAt.Should().BeNull();
        _settingsServiceMock.Verify(s => s.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckAutoDisableAsync_WhenVerboseEnabledButNoTimestamp_AppliesDebugLevel()
    {
        // Arrange
        var service = CreateService();
        _settings.VerboseLogging = true;
        _settings.VerboseLoggingEnabledAt = null; // No timestamp (edge case)

        // Act
        await service.CheckAutoDisableAsync();

        // Assert
        _levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Debug);
        _settingsServiceMock.Verify(s => s.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckAutoDisableAsync_WhenJustBeforeTimeout_DoesNotAutoDisable()
    {
        // Arrange
        var service = CreateService();
        _settings.VerboseLogging = true;
        _settings.VerboseLoggingEnabledAt = DateTime.UtcNow.AddHours(-47).AddMinutes(-59); // Just under 48 hours ago

        // Act
        await service.CheckAutoDisableAsync();

        // Assert - Should still be enabled (under 48h timeout)
        _levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Debug);
        _settings.VerboseLogging.Should().BeTrue();
    }

    [Fact]
    public void SetVerboseLogging_WhenEnablingTwice_UpdatesTimestamp()
    {
        // Arrange
        var service = CreateService();
        var originalTimestamp = DateTime.UtcNow.AddHours(-10);
        _settings.VerboseLogging = true;
        _settings.VerboseLoggingEnabledAt = originalTimestamp;

        // Act
        service.SetVerboseLogging(true);

        // Assert - Timestamp should be updated to now
        _settings.VerboseLoggingEnabledAt.Should().NotBe(originalTimestamp);
        _settings.VerboseLoggingEnabledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SetVerboseLogging_WhenDisablingAlreadyDisabled_DoesNotThrow()
    {
        // Arrange
        var service = CreateService();
        _settings.VerboseLogging = false;
        _settings.VerboseLoggingEnabledAt = null;

        // Act & Assert - Should not throw
        var act = () => service.SetVerboseLogging(false);
        act.Should().NotThrow();
        _settings.VerboseLogging.Should().BeFalse();
    }

    private LoggingService CreateService()
    {
        return new LoggingService(
            _settingsServiceMock.Object,
            _loggerMock.Object,
            _levelSwitch);
    }
}

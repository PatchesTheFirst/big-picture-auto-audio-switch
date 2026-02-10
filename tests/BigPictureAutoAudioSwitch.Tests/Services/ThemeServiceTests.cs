using BigPictureAutoAudioSwitch.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BigPictureAutoAudioSwitch.Tests.Services;

public class ThemeServiceTests
{
    private readonly Mock<ILogger<ThemeService>> _loggerMock;

    public ThemeServiceTests()
    {
        _loggerMock = new Mock<ILogger<ThemeService>>();
    }

    [Fact]
    public void Constructor_DetectsWindowsTheme()
    {
        // Arrange & Act
        var service = new ThemeService(_loggerMock.Object);

        // Assert - IsDarkMode should be set (doesn't throw)
        // The actual value depends on system settings
        service.Should().NotBeNull();
        // IsDarkMode is a bool, so it's always valid - just verify we can read it
        _ = service.IsDarkMode;
    }

    [Fact]
    public void StartListening_CanBeCalledMultipleTimes()
    {
        // Arrange
        var service = new ThemeService(_loggerMock.Object);

        // Act - Should not throw when called multiple times
        service.StartListening();
        service.StartListening();

        // Cleanup
        service.StopListening();
    }

    [Fact]
    public void StopListening_CanBeCalledMultipleTimes()
    {
        // Arrange
        var service = new ThemeService(_loggerMock.Object);
        service.StartListening();

        // Act - Should not throw when called multiple times
        service.StopListening();
        service.StopListening();
    }

    [Fact]
    public void StopListening_CanBeCalledWithoutStarting()
    {
        // Arrange
        var service = new ThemeService(_loggerMock.Object);

        // Act - Should not throw
        service.StopListening();
    }

    [Fact]
    public void ThemeChanged_EventCanBeSubscribed()
    {
        // Arrange
        var service = new ThemeService(_loggerMock.Object);
        EventHandler<bool>? handler = null;

        // Act - Should not throw
        handler = (s, isDark) => { };
        service.ThemeChanged += handler;
        service.ThemeChanged -= handler;

        // Assert - Event subscription works
        service.Should().NotBeNull();
    }
}

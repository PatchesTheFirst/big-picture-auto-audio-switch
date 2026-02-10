using BigPictureAutoAudioSwitch.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Moq;

namespace BigPictureAutoAudioSwitch.Tests.Services;

/// <summary>
/// Integration tests for StartupService.
/// These tests interact with the Windows Registry and clean up after themselves.
/// </summary>
public class StartupServiceTests : IDisposable
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "BigPictureAutoAudioSwitch";
    
    private readonly Mock<ILogger<StartupService>> _loggerMock;
    private readonly StartupService _startupService;
    private readonly string? _originalValue;

    public StartupServiceTests()
    {
        _loggerMock = new Mock<ILogger<StartupService>>();
        _startupService = new StartupService(_loggerMock.Object);
        
        // Save original value to restore after tests
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
        _originalValue = key?.GetValue(AppName) as string;
    }

    public void Dispose()
    {
        // Restore original state
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
        if (key != null)
        {
            if (_originalValue != null)
            {
                key.SetValue(AppName, _originalValue);
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task IsEnabledAsync_WhenNotRegistered_ReturnsFalse()
    {
        // Arrange - Ensure no registry entry exists
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
        {
            key?.DeleteValue(AppName, false);
        }

        // Act
        var result = await _startupService.IsEnabledAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEnabledAsync_WhenRegistered_ReturnsTrue()
    {
        // Arrange - Create a registry entry
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
        {
            key?.SetValue(AppName, "\"C:\\SomePath\\App.exe\"");
        }

        // Act
        var result = await _startupService.IsEnabledAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetEnabledAsync_WhenTrue_CreatesRegistryEntry()
    {
        // Arrange - Ensure clean state
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
        {
            key?.DeleteValue(AppName, false);
        }

        // Act
        await _startupService.SetEnabledAsync(true);

        // Assert
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
        {
            var value = key?.GetValue(AppName) as string;
            value.Should().NotBeNullOrEmpty();
            value.Should().Contain(Environment.ProcessPath ?? "dotnet");
        }
    }

    [Fact]
    public async Task SetEnabledAsync_WhenFalse_RemovesRegistryEntry()
    {
        // Arrange - Create an entry first
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
        {
            key?.SetValue(AppName, "\"C:\\SomePath\\App.exe\"");
        }

        // Act
        await _startupService.SetEnabledAsync(false);

        // Assert
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
        {
            var value = key?.GetValue(AppName);
            value.Should().BeNull();
        }
    }

    [Fact]
    public async Task IsEnabledAndValidAsync_WhenNotRegistered_ReturnsFalse()
    {
        // Arrange - Ensure no registry entry exists
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
        {
            key?.DeleteValue(AppName, false);
        }

        // Act
        var result = await _startupService.IsEnabledAndValidAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEnabledAndValidAsync_WhenPathMatches_ReturnsTrue()
    {
        // Arrange - Register with current process path
        var currentPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentPath))
        {
            // Skip test if we can't get process path (shouldn't happen)
            return;
        }

        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
        {
            key?.SetValue(AppName, $"\"{currentPath}\"");
        }

        // Act
        var result = await _startupService.IsEnabledAndValidAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEnabledAndValidAsync_WhenPathMismatch_ReturnsFalse()
    {
        // Arrange - Register with a different path
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
        {
            key?.SetValue(AppName, "\"C:\\DifferentPath\\OtherApp.exe\"");
        }

        // Act
        var result = await _startupService.IsEnabledAndValidAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetEnabledAsync_ThenIsEnabledAndValidAsync_ReturnsTrue()
    {
        // Arrange - Clean state
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
        {
            key?.DeleteValue(AppName, false);
        }

        // Act
        await _startupService.SetEnabledAsync(true);
        var result = await _startupService.IsEnabledAndValidAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEnabledAndValidAsync_CaseInsensitivePathComparison()
    {
        // Arrange - Register with different case
        var currentPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentPath))
        {
            return;
        }

        // Use opposite case
        var alteredPath = currentPath.ToUpperInvariant();
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
        {
            key?.SetValue(AppName, $"\"{alteredPath}\"");
        }

        // Act
        var result = await _startupService.IsEnabledAndValidAsync();

        // Assert - Should still match (case-insensitive on Windows)
        result.Should().BeTrue();
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BigPictureAutoAudioSwitch.Services;

/// <summary>
/// Manages app startup via Windows Registry for non-packaged apps.
/// For MSIX packages, startup is handled via the package manifest.
/// </summary>
public class StartupService : IStartupService
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private static string AppName => AppConstants.AppName;

    private readonly ILogger<StartupService> _logger;

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsEnabledAsync()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var value = key?.GetValue(AppName);
            var isEnabled = value != null;
            _logger.LogDebug("Startup is {Status}", isEnabled ? "enabled" : "disabled");
            return Task.FromResult(isEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check startup status");
            return Task.FromResult(false);
        }
    }

    public Task<bool> IsEnabledAndValidAsync()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var registeredPath = key?.GetValue(AppName) as string;
            
            if (string.IsNullOrEmpty(registeredPath))
            {
                _logger.LogDebug("Startup is disabled (no registry entry)");
                return Task.FromResult(false);
            }

            var currentPath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentPath))
            {
                _logger.LogWarning("Could not get current process path");
                return Task.FromResult(false);
            }

            // Registry value is quoted, so compare with quoted current path
            var expectedPath = $"\"{currentPath}\"";
            var isValid = string.Equals(registeredPath, expectedPath, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Startup path mismatch - registered: {RegisteredPath}, current: {CurrentPath}",
                    registeredPath,
                    expectedPath);
            }
            else
            {
                _logger.LogDebug("Startup is enabled and path is valid");
            }

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check startup path validity");
            return Task.FromResult(false);
        }
    }

    public Task SetEnabledAsync(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
            {
                _logger.LogWarning("Failed to open registry key: {RegistryKeyPath}", RegistryKeyPath);
                return Task.CompletedTask;
            }

            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                    _logger.LogInformation("Enabled startup with path: {ExePath}", exePath);
                }
                else
                {
                    _logger.LogWarning("Could not get process path for startup registration");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
                _logger.LogInformation("Disabled startup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to {Action} startup", enabled ? "enable" : "disable");
        }

        return Task.CompletedTask;
    }
}

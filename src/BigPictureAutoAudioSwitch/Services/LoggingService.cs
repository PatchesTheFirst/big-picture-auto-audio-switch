using System.IO;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace BigPictureAutoAudioSwitch.Services;

public class LoggingService : ILoggingService
{
    private static string LogsFolder => AppConstants.LogsFolder;

    private static TimeSpan AutoDisableTimeout => AppConstants.VerboseLoggingTimeout;

    private readonly ISettingsService _settingsService;
    private readonly ILogger<LoggingService> _logger;
    private readonly LoggingLevelSwitch _levelSwitch;

    public bool IsVerboseLogging => _settingsService.Settings.VerboseLogging;

    public LoggingService(
        ISettingsService settingsService,
        ILogger<LoggingService> logger,
        LoggingLevelSwitch levelSwitch)
    {
        _settingsService = settingsService;
        _logger = logger;
        _levelSwitch = levelSwitch;
    }

    public void SetVerboseLogging(bool enabled)
    {
        var wasEnabled = _settingsService.Settings.VerboseLogging;
        _settingsService.Settings.VerboseLogging = enabled;

        if (enabled)
        {
            // Only stamp on the off->on transition so re-saving settings
            // doesn't extend the 48-hour auto-disable window
            if (!wasEnabled || _settingsService.Settings.VerboseLoggingEnabledAt == null)
            {
                _settingsService.Settings.VerboseLoggingEnabledAt = DateTime.UtcNow;
                _logger.LogInformation("Verbose logging enabled");
            }
            _levelSwitch.MinimumLevel = LogEventLevel.Debug;
        }
        else
        {
            _settingsService.Settings.VerboseLoggingEnabledAt = null;
            _levelSwitch.MinimumLevel = LogEventLevel.Information;
            
            if (wasEnabled)
            {
                _logger.LogInformation("Verbose logging disabled");
            }
        }
    }

    public string GetLogsFolder() => LogsFolder;

    public async Task CheckAutoDisableAsync(CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.Settings;
        
        if (!settings.VerboseLogging || !settings.VerboseLoggingEnabledAt.HasValue)
        {
            // Apply current level based on settings
            _levelSwitch.MinimumLevel = settings.VerboseLogging 
                ? LogEventLevel.Debug 
                : LogEventLevel.Information;
            return;
        }

        var enabledAt = settings.VerboseLoggingEnabledAt.Value;
        var elapsed = DateTime.UtcNow - enabledAt;

        if (elapsed > AutoDisableTimeout)
        {
            _logger.LogInformation(
                "Verbose logging auto-disabled after {Hours:F1} hours (enabled at {EnabledAt:u})",
                elapsed.TotalHours,
                enabledAt);

            settings.VerboseLogging = false;
            settings.VerboseLoggingEnabledAt = null;
            _levelSwitch.MinimumLevel = LogEventLevel.Information;
            
            await _settingsService.SaveAsync(cancellationToken);
        }
        else
        {
            // Still within timeout, keep verbose logging enabled
            _levelSwitch.MinimumLevel = LogEventLevel.Debug;
            
            var remainingHours = (AutoDisableTimeout - elapsed).TotalHours;
            _logger.LogDebug(
                "Verbose logging will auto-disable in {RemainingHours:F1} hours",
                remainingHours);
        }
    }
}

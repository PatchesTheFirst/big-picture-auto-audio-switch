using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BigPictureAutoAudioSwitch.Services;

public class SettingsService : ISettingsService
{
    private static string SettingsFolder => AppConstants.AppDataFolder;
    
    private static string SettingsFile => AppConstants.SettingsFile;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<SettingsService> _logger;
    private readonly object _settingsLock = new();

    public AppSettings Settings { get; private set; } = new();

    public event EventHandler? SettingsChanged;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = await File.ReadAllTextAsync(SettingsFile, cancellationToken);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null)
                {
                    lock (_settingsLock)
                    {
                        Settings = settings;
                    }
                    _logger.LogInformation("Settings loaded from {SettingsFile}", SettingsFile);
                }
            }
            else
            {
                _logger.LogInformation("No settings file found, using defaults");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Settings load was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {SettingsFile}, using defaults", SettingsFile);
            lock (_settingsLock)
            {
                Settings = new AppSettings();
            }
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
                _logger.LogDebug("Created settings folder: {SettingsFolder}", SettingsFolder);
            }

            string json;
            lock (_settingsLock)
            {
                json = JsonSerializer.Serialize(Settings, JsonOptions);
            }
            
            await File.WriteAllTextAsync(SettingsFile, json, cancellationToken);
            _logger.LogInformation("Settings saved to {SettingsFile}", SettingsFile);
            
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Settings save was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {SettingsFile}", SettingsFile);
        }
    }
}

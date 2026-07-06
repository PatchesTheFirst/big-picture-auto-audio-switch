using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BigPictureAutoAudioSwitch.Services;

public class UpdateCheckService : IUpdateCheckService, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UpdateCheckService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Version _currentVersion;
    private CancellationTokenSource? _loopCts;
    private string? _lastNotifiedVersion;
    private bool _disposed;

    public UpdateInfo? AvailableUpdate { get; private set; }

    public event EventHandler<UpdateInfo>? UpdateAvailable;

    public UpdateCheckService(
        ISettingsService settingsService,
        INotificationService notificationService,
        ILogger<UpdateCheckService> logger,
        HttpMessageHandler? httpMessageHandler = null,
        Version? currentVersion = null)
    {
        _settingsService = settingsService;
        _notificationService = notificationService;
        _logger = logger;
        _currentVersion = currentVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version
            ?? new Version(0, 0, 0);

        _httpClient = httpMessageHandler != null ? new HttpClient(httpMessageHandler) : new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        // GitHub API rejects requests without a User-Agent
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppConstants.AppName}/{_currentVersion}");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public void Start()
    {
        if (_loopCts != null) return;

        _logger.LogInformation("Starting periodic update checks (every {Interval} hours)",
            AppConstants.UpdateCheckInterval.TotalHours);
        _loopCts = new CancellationTokenSource();
        _ = RunPeriodicChecksAsync(_loopCts.Token);
    }

    public void Stop()
    {
        if (_loopCts == null) return;

        _logger.LogInformation("Stopping periodic update checks");
        _loopCts.Cancel();
        _loopCts.Dispose();
        _loopCts = null;
    }

    private async Task RunPeriodicChecksAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AppConstants.UpdateCheckInitialDelay, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_settingsService.Settings.CheckForUpdates)
                {
                    await CheckForUpdateAsync(cancellationToken);
                }
                else
                {
                    _logger.LogDebug("Update checks are disabled in settings, skipping");
                }

                await Task.Delay(AppConstants.UpdateCheckInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Periodic update checks cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Periodic update check loop failed");
        }
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(AppConstants.LatestReleaseApiUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Update check failed with HTTP {StatusCode}", (int)response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagElement) ? tagElement.GetString() : null;
            var releaseUrl = root.TryGetProperty("html_url", out var urlElement) ? urlElement.GetString() : null;

            if (string.IsNullOrWhiteSpace(tagName))
            {
                _logger.LogDebug("Update check response did not contain a tag name");
                return null;
            }

            if (!TryParseVersionTag(tagName, out var latestVersion))
            {
                _logger.LogDebug("Could not parse release tag '{TagName}' as a version", tagName);
                return null;
            }

            if (Normalize(latestVersion) <= Normalize(_currentVersion))
            {
                _logger.LogDebug("No update available. Latest release: v{Latest}, current: v{Current}",
                    latestVersion, _currentVersion);
                return null;
            }

            var info = new UpdateInfo(
                Normalize(latestVersion).ToString(3),
                string.IsNullOrWhiteSpace(releaseUrl) ? $"{AppConstants.GitHubRepoUrl}/releases/latest" : releaseUrl);

            AvailableUpdate = info;
            _logger.LogInformation("Update available: v{Latest} (current: v{Current})", info.Version, _currentVersion);
            UpdateAvailable?.Invoke(this, info);

            // Notify at most once per version so the daily check doesn't nag
            if (_lastNotifiedVersion != info.Version)
            {
                _lastNotifiedVersion = info.Version;
                _notificationService.ShowUpdateAvailable(info.Version, info.ReleaseUrl);
            }

            return info;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Network errors, timeouts, malformed JSON - never let an update check disturb the app
            _logger.LogDebug(ex, "Update check failed");
            return null;
        }
    }

    private static bool TryParseVersionTag(string tagName, out Version version)
    {
        var trimmed = tagName.Trim().TrimStart('v', 'V');
        return Version.TryParse(trimmed, out version!);
    }

    /// <summary>
    /// Normalizes to Major.Minor.Build so that "1.0.1" and "1.0.1.0" compare as equal.
    /// </summary>
    private static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(version.Build, 0));

    public void Dispose()
    {
        if (_disposed) return;

        Stop();
        _httpClient.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

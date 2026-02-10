using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;

namespace BigPictureAutoAudioSwitch.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public void ShowAudioSwitched(string deviceName, bool isBigPictureMode)
    {
        var title = isBigPictureMode 
            ? "Big Picture Mode Detected" 
            : "Big Picture Mode Closed";
            
        var message = $"Audio switched to: {deviceName}";

        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
            
            _logger.LogDebug("Successfully displayed notification: '{Title}' - '{Message}'", title, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to display Windows toast notification (title: '{Title}'). Notifications may be disabled in Windows Settings.", title);
        }
    }

    public void ShowDeviceMissing(string deviceName)
    {
        const string title = "Audio Device Unavailable";
        var message = $"The configured audio device '{deviceName}' is not available. It may be disconnected or disabled. Please check your audio settings.";
        
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
            
            _logger.LogDebug("Successfully displayed device missing notification for: '{DeviceName}'", deviceName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to display device missing notification for '{DeviceName}'. Notifications may be disabled in Windows Settings.", deviceName);
        }
    }
}

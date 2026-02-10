using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Application = System.Windows.Application;

namespace BigPictureAutoAudioSwitch.Services;

public class ThemeService : IThemeService
{
    private const string ThemeRegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ThemeRegistryValueName = "AppsUseLightTheme";
    private const string DarkThemePath = "pack://application:,,,/Views/Themes/ThemeColors.Dark.xaml";
    private const string LightThemePath = "pack://application:,,,/Views/Themes/ThemeColors.Light.xaml";

    private readonly ILogger<ThemeService> _logger;
    private bool _isListening;

    public bool IsDarkMode { get; private set; }

    public event EventHandler<bool>? ThemeChanged;

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
        IsDarkMode = DetectWindowsTheme();
    }

    public void StartListening()
    {
        if (_isListening) return;
        
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _isListening = true;
        _logger.LogDebug("Started listening for theme changes");
    }

    public void StopListening()
    {
        if (!_isListening) return;
        
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _isListening = false;
        _logger.LogDebug("Stopped listening for theme changes");
    }

    public void ApplyTheme()
    {
        IsDarkMode = DetectWindowsTheme();
        
        var app = Application.Current;
        if (app == null) return;

        var themeDictionary = IsDarkMode
            ? new Uri("Views/Themes/ThemeColors.Dark.xaml", UriKind.Relative)
            : new Uri("Views/Themes/ThemeColors.Light.xaml", UriKind.Relative);

        // Find and replace the theme dictionary
        var existingThemeDictionary = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("ThemeColors.") == true);

        if (existingThemeDictionary != null)
        {
            var index = app.Resources.MergedDictionaries.IndexOf(existingThemeDictionary);
            app.Resources.MergedDictionaries[index] = new ResourceDictionary { Source = themeDictionary };
        }
        else
        {
            // Insert theme dictionary at the beginning so it can be overridden by other dictionaries
            app.Resources.MergedDictionaries.Insert(0, new ResourceDictionary { Source = themeDictionary });
        }

        _logger.LogInformation("Applied {Theme} theme", IsDarkMode ? "dark" : "light");
    }

    public void ApplyThemeToContextMenu(ContextMenu contextMenu)
    {
        if (contextMenu == null) return;

        var themeDictionaryUri = IsDarkMode
            ? new Uri(DarkThemePath, UriKind.Absolute)
            : new Uri(LightThemePath, UriKind.Absolute);

        // Find and replace existing theme dictionary in context menu resources
        var existingTheme = contextMenu.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("ThemeColors.") == true);

        if (existingTheme != null)
        {
            var index = contextMenu.Resources.MergedDictionaries.IndexOf(existingTheme);
            contextMenu.Resources.MergedDictionaries[index] = new ResourceDictionary { Source = themeDictionaryUri };
        }
        else
        {
            // Add theme dictionary to context menu resources
            contextMenu.Resources.MergedDictionaries.Insert(0, new ResourceDictionary { Source = themeDictionaryUri });
        }

        _logger.LogDebug("Applied {Theme} theme to context menu", IsDarkMode ? "dark" : "light");
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General) return;

        var newIsDarkMode = DetectWindowsTheme();
        if (newIsDarkMode != IsDarkMode)
        {
            _logger.LogInformation("Windows theme changed to {Theme}", newIsDarkMode ? "dark" : "light");
            IsDarkMode = newIsDarkMode;
            
            // Apply theme on UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ApplyTheme();
                ThemeChanged?.Invoke(this, newIsDarkMode);
            });
        }
    }

    private bool DetectWindowsTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ThemeRegistryKeyPath);
            var value = key?.GetValue(ThemeRegistryValueName);
            
            if (value is int intValue)
            {
                // AppsUseLightTheme: 0 = dark, 1 = light
                return intValue == 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Windows theme from registry, defaulting to dark");
        }

        // Default to dark theme if we can't detect
        return true;
    }
}

using System.Windows.Controls;

namespace BigPictureAutoAudioSwitch.Services;

public interface IThemeService
{
    /// <summary>
    /// Gets whether the current Windows theme is dark mode.
    /// </summary>
    bool IsDarkMode { get; }

    /// <summary>
    /// Event raised when the Windows theme changes.
    /// </summary>
    event EventHandler<bool>? ThemeChanged;

    /// <summary>
    /// Applies the current theme to the application.
    /// </summary>
    void ApplyTheme();

    /// <summary>
    /// Applies the current theme to a context menu.
    /// Context menus are not in the visual tree, so they need separate theme application.
    /// </summary>
    /// <param name="contextMenu">The context menu to apply the theme to.</param>
    void ApplyThemeToContextMenu(ContextMenu contextMenu);

    /// <summary>
    /// Starts listening for theme changes.
    /// </summary>
    void StartListening();

    /// <summary>
    /// Stops listening for theme changes.
    /// </summary>
    void StopListening();
}

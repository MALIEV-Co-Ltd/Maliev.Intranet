namespace Maliev.Intranet.Client.Layout;

/// <summary>
/// Defines layout operations for managing theme state, such as toggling dark mode.
/// </summary>
public interface IThemeLayout
{
    /// <summary>
    /// Raised when the theme changes between light and dark modes.
    /// </summary>
    event Action? OnThemeChanged;

    /// <summary>
    /// Gets a value indicating whether the layout is currently in dark mode.
    /// </summary>
    bool IsDarkMode { get; }

    /// <summary>
    /// Toggles the layout theme between light and dark modes.
    /// </summary>
    void ToggleTheme();
}

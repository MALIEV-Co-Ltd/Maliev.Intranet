namespace Maliev.Intranet.Client.Layout;

/// <summary>
/// Interface for layout components that support theme switching.
/// </summary>
public interface IThemeLayout
{
    /// <summary>Event raised when the theme has changed.</summary>
    event Action? OnThemeChanged;
    /// <summary>Gets a value indicating whether dark mode is currently active.</summary>
    bool IsDarkMode { get; }
    /// <summary>Toggles between light and dark theme modes.</summary>
    void ToggleTheme();
}


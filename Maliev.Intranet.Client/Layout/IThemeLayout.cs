namespace Maliev.Intranet.Client.Layout;

public interface IThemeLayout
{
    event Action? OnThemeChanged;
    bool IsDarkMode { get; }
    void ToggleTheme();
}

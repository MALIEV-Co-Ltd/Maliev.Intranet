using MudBlazor;

namespace Maliev.Intranet.Client;

/// <summary>
/// Centralized theme configuration for the Maliev Intranet.
/// </summary>
public static class ThemeConfiguration
{
    /// <summary>
    /// The custom Maliev theme instance.
    /// </summary>
    public static readonly MudTheme MalievTheme = new()
    {
        PaletteDark = new PaletteDark()
        {
            Primary = "#2f81f7", // GitHub Blue
            Secondary = "#8b949e", // GitHub Dimmed Text
            Tertiary = "#238636", // GitHub Green
            Background = "#0d1117", // GitHub Main Background
            Surface = "#161b22", // GitHub Secondary Background (Cards)
            AppbarBackground = "#010409", // Darker Header
            DrawerBackground = "#0d1117", // Sidebar match main
            TextPrimary = "#c9d1d9", // High Contrast Text
            TextSecondary = "#8b949e", // Muted Text
            Success = "#238636",
            Warning = "#d29922",
            Error = "#f85149",
            Info = "#58a6ff",
            Divider = "#30363d", // Subtle borders
            ActionDefault = "#8b949e",
            LinesDefault = "#30363d",
            TableLines = "#30363d",
            DrawerText = "#c9d1d9",
            AppbarText = "#c9d1d9"
        },
        PaletteLight = new PaletteLight()
        {
            Primary = "#0969da",
            Secondary = "#57606a",
            Tertiary = "#1f883d",
            Background = "#ffffff",
            Surface = "#f6f8fa",
            AppbarBackground = "#f6f8fa", // Light Header on Light Mode
            DrawerBackground = "#f6f8fa",
            TextPrimary = "#24292f",
            TextSecondary = "#57606a",
            Success = "#1a7f37",
            Warning = "#9a6700",
            Error = "#cf222e",
            Info = "#0969da",
            Divider = "#d0d7de",
            ActionDefault = "#57606a",
            LinesDefault = "#d0d7de",
            TableLines = "#d0d7de",
            DrawerText = "#24292f",
            AppbarText = "#24292f" // Dark text on light header
        }
    };
}

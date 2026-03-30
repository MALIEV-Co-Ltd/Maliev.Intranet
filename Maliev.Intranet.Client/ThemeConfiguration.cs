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
    public static readonly MudTheme MalievTheme = CreateTheme();

    private static MudTheme CreateTheme()
    {
        var theme = new MudTheme();

        // ── Font stack ───────────────────────────────────────────────────────────
        string[] fontStack = ["Noto Sans", "Noto Sans Thai", "sans-serif"];

        // ── Typography ───────────────────────────────────────────────────────────
        // Body / Default
        theme.Typography.Default.FontFamily = fontStack;
        theme.Typography.Default.FontSize = "13px";

        theme.Typography.Body1.FontFamily = fontStack;
        theme.Typography.Body1.FontSize = "13px";

        theme.Typography.Body2.FontFamily = fontStack;
        theme.Typography.Body2.FontSize = "12px";

        // Headings
        theme.Typography.H1.FontFamily = fontStack;
        theme.Typography.H1.FontSize = "28px";

        theme.Typography.H2.FontFamily = fontStack;
        theme.Typography.H2.FontSize = "22px";

        theme.Typography.H3.FontFamily = fontStack;
        theme.Typography.H3.FontSize = "18px";

        theme.Typography.H4.FontFamily = fontStack;
        theme.Typography.H4.FontSize = "16px";

        theme.Typography.H5.FontFamily = fontStack;
        theme.Typography.H5.FontSize = "15px";

        theme.Typography.H6.FontFamily = fontStack;
        theme.Typography.H6.FontSize = "14px";

        // Supporting text
        theme.Typography.Subtitle1.FontFamily = fontStack;
        theme.Typography.Subtitle1.FontSize = "14px";

        theme.Typography.Subtitle2.FontFamily = fontStack;
        theme.Typography.Subtitle2.FontSize = "13px";

        theme.Typography.Caption.FontFamily = fontStack;
        theme.Typography.Caption.FontSize = "11px";

        theme.Typography.Overline.FontFamily = fontStack;
        theme.Typography.Overline.FontSize = "11px";

        // Interactive
        theme.Typography.Button.FontFamily = fontStack;
        theme.Typography.Button.FontSize = "13px";

        // ── Layout Properties ────────────────────────────────────────────────────
        theme.LayoutProperties.DefaultBorderRadius = "6px";

        // ── Typography weights & line-heights ───────────────────────────────────
        // Note: MudBlazor 9.x uses string for FontWeight and LineHeight
        theme.Typography.Default.FontWeight = "400";
        theme.Typography.Default.LineHeight = "1.4";

        theme.Typography.Body1.FontWeight = "400";
        theme.Typography.Body1.LineHeight = "1.4";

        theme.Typography.Body2.FontWeight = "400";
        theme.Typography.Body2.LineHeight = "1.4";

        theme.Typography.H1.FontWeight = "700";
        theme.Typography.H1.LineHeight = "1.2";

        theme.Typography.H2.FontWeight = "700";
        theme.Typography.H2.LineHeight = "1.25";

        theme.Typography.H3.FontWeight = "600";
        theme.Typography.H3.LineHeight = "1.3";

        theme.Typography.H4.FontWeight = "600";
        theme.Typography.H4.LineHeight = "1.35";

        theme.Typography.H5.FontWeight = "600";
        theme.Typography.H5.LineHeight = "1.35";

        theme.Typography.H6.FontWeight = "600";
        theme.Typography.H6.LineHeight = "1.4";

        theme.Typography.Subtitle1.FontWeight = "500";
        theme.Typography.Subtitle1.LineHeight = "1.4";

        theme.Typography.Subtitle2.FontWeight = "500";
        theme.Typography.Subtitle2.LineHeight = "1.4";

        theme.Typography.Caption.FontWeight = "400";
        theme.Typography.Caption.LineHeight = "1.3";

        theme.Typography.Overline.FontWeight = "600";
        theme.Typography.Overline.LineHeight = "1.3";
        theme.Typography.Overline.LetterSpacing = "0.5px";

        theme.Typography.Button.FontWeight = "600";
        theme.Typography.Button.LineHeight = "1.4";
        theme.Typography.Button.TextTransform = "none";

        // ── Palette (light) ──────────────────────────────────────────────────────
        theme.PaletteLight.Primary = "#2563eb";
        theme.PaletteLight.PrimaryLighten = "rgba(37,99,235,0.05)";
        theme.PaletteLight.PrimaryDarken = "#1d4ed8";
        theme.PaletteLight.Secondary = "#64748b";
        theme.PaletteLight.SecondaryLighten = "#f1f5f9";

        theme.PaletteDark.Primary = "#3b82f6";
        theme.PaletteDark.PrimaryLighten = "rgba(59,130,246,0.10)";
        theme.PaletteDark.PrimaryDarken = "#60a5fa";
        theme.PaletteDark.Secondary = "#94a3b8";
        theme.PaletteDark.SecondaryLighten = "#1e293b";

        theme.PaletteLight.Surface = "#ffffff";
        theme.PaletteLight.Background = "#f1f5f9";
        theme.PaletteLight.TextPrimary = "#1a1a1a";
        theme.PaletteLight.TextSecondary = "#6b7280";

        theme.PaletteDark.Surface = "#1e2130";
        theme.PaletteDark.Background = "#131620";
        theme.PaletteDark.TextPrimary = "#f1f5f9";
        theme.PaletteDark.TextSecondary = "#94a3b8";

        theme.PaletteLight.Divider = "#e4e4e7";
        theme.PaletteLight.LinesDefault = "#e4e4e7";
        theme.PaletteLight.LinesInputs = "#d4d4d8";

        theme.PaletteDark.Divider = "#2d3148";
        theme.PaletteDark.LinesDefault = "#2d3148";
        theme.PaletteDark.LinesInputs = "#3d4160";

        theme.PaletteLight.Success = "#059669";
        theme.PaletteLight.Warning = "#d97706";
        theme.PaletteLight.Error = "#dc2626";

        theme.PaletteDark.Success = "#10b981";
        theme.PaletteDark.Warning = "#f59e0b";
        theme.PaletteDark.Error = "#ef4444";

        return theme;
    }
}

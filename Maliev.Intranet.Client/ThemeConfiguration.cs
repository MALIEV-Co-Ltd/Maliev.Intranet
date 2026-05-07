using MudBlazor;
using MudBlazor.Utilities;

namespace Maliev.Intranet.Client;

/// <summary>
/// Centralized MudBlazor theme configuration for the Maliev Intranet.
/// Split into Light Mode and Dark Mode sections; Typography, LayoutProperties,
/// ZIndex, Shadows, and PseudoCss are shared across both modes.
/// </summary>
public static class ThemeConfiguration
{
    /// <summary>The custom Maliev theme instance consumed by <see cref="MudThemeProvider"/>.</summary>
    public static readonly MudTheme MalievTheme = CreateTheme();

    private static MudTheme CreateTheme()
    {
        var theme = new MudTheme();

        string[] fontStack = ["Geist", "Noto Sans Thai", "sans-serif"];

        // ═══════════════════════════════════════════════════════════════════
        //  LIGHT MODE — Palette
        // ═══════════════════════════════════════════════════════════════════
        theme.PaletteLight = new PaletteLight
        {
            // ── Core palette (MudColor) ─────────────────────────────────
            Primary = new MudColor("#171717"),
            PrimaryContrastText = new MudColor("#ffffff"),
            PrimaryLighten = "#f5f5f5",
            PrimaryDarken = "#000000",

            Secondary = new MudColor("#4d4d4d"),
            SecondaryContrastText = new MudColor("#ffffff"),
            SecondaryLighten = "#fafafa",
            SecondaryDarken = "#171717",

            Tertiary = new MudColor("#0a72ef"),
            TertiaryContrastText = new MudColor("#ffffff"),
            TertiaryLighten = "#ebf5ff",
            TertiaryDarken = "#0068d6",

            Info = new MudColor("#0072f5"),
            InfoContrastText = new MudColor("#ffffff"),
            InfoLighten = "#ebf5ff",
            InfoDarken = "#0068d6",

            Success = new MudColor("#059669"),
            SuccessContrastText = new MudColor("#ffffff"),
            SuccessLighten = "#d1fae5",
            SuccessDarken = "#047857",

            Warning = new MudColor("#d97706"),
            WarningContrastText = new MudColor("#ffffff"),
            WarningLighten = "#fef3c7",
            WarningDarken = "#b45309",

            Error = new MudColor("#dc2626"),
            ErrorContrastText = new MudColor("#ffffff"),
            ErrorLighten = "#fee2e2",
            ErrorDarken = "#b91c1c",

            Dark = new MudColor("#1e293b"),
            DarkContrastText = new MudColor("#ffffff"),
            DarkLighten = "#334155",
            DarkDarken = "#0f172a",

            Black = new MudColor("#000000"),
            White = new MudColor("#ffffff"),

            // ── Text (MudColor) ─────────────────────────────────────────
            TextPrimary = new MudColor("#171717"),
            TextSecondary = new MudColor("#4d4d4d"),
            TextDisabled = new MudColor("#808080"),

            // ── Action / interaction (MudColor) ─────────────────────────
            ActionDefault = new MudColor("#666666"),
            ActionDisabled = new MudColor("#808080"),
            ActionDisabledBackground = new MudColor("#ebebeb"),

            // ── Surfaces (MudColor) ─────────────────────────────────────
            Background = new MudColor("#ffffff"),
            BackgroundGray = new MudColor("#fafafa"),
            Surface = new MudColor("#ffffff"),
            DrawerBackground = new MudColor("#ffffff"),
            DrawerText = new MudColor("#171717"),
            DrawerIcon = new MudColor("#666666"),

            // ── App bar (MudColor) ──────────────────────────────────────
            AppbarBackground = new MudColor("#ffffff"),
            AppbarText = new MudColor("#171717"),

            // ── Lines / dividers (MudColor) ──────────────────────────────
            LinesDefault = new MudColor("#ebebeb"),
            LinesInputs = new MudColor("#ebebeb"),
            TableLines = new MudColor("#ebebeb"),
            TableStriped = new MudColor("#fafafa"),
            TableHover = new MudColor("#fafafa"),
            Divider = new MudColor("#ebebeb"),
            DividerLight = new MudColor("#f5f5f5"),

            // ── Misc (MudColor) ─────────────────────────────────────────
            Skeleton = new MudColor("#ebebeb"),

            // ── Gray / overlay scale ───────────────────────────────────
            GrayDefault = "#808080",
            GrayLight = "#ebebeb",
            GrayLighter = "#fafafa",
            GrayDark = "#4d4d4d",
            GrayDarker = "#171717",
            OverlayDark = "rgba(0,0,0,0.5)",
            OverlayLight = "rgba(255,255,255,0.5)",

            // ── Opacity / ripple (double) ──────────────────────────────
            BorderOpacity = 1.0,
            HoverOpacity = 0.04,
            RippleOpacity = 0.0,
            RippleOpacitySecondary = 0.0,
        };

        // ═══════════════════════════════════════════════════════════════════
        //  LIGHT MODE — Typography
        // ═══════════════════════════════════════════════════════════════════
        theme.Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = fontStack,
                FontSize = "13px",
                FontWeight = "400",
                LineHeight = "1.4",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            H1 = new H1Typography
            {
                FontFamily = fontStack,
                FontSize = "28px",
                FontWeight = "600",
                LineHeight = "1.2",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            H2 = new H2Typography
            {
                FontFamily = fontStack,
                FontSize = "22px",
                FontWeight = "600",
                LineHeight = "1.25",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            H3 = new H3Typography
            {
                FontFamily = fontStack,
                FontSize = "18px",
                FontWeight = "600",
                LineHeight = "1.3",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            H4 = new H4Typography
            {
                FontFamily = fontStack,
                FontSize = "16px",
                FontWeight = "600",
                LineHeight = "1.35",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            H5 = new H5Typography
            {
                FontFamily = fontStack,
                FontSize = "15px",
                FontWeight = "600",
                LineHeight = "1.35",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            H6 = new H6Typography
            {
                FontFamily = fontStack,
                FontSize = "14px",
                FontWeight = "600",
                LineHeight = "1.4",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontFamily = fontStack,
                FontSize = "14px",
                FontWeight = "500",
                LineHeight = "1.4",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            Subtitle2 = new Subtitle2Typography
            {
                FontFamily = fontStack,
                FontSize = "13px",
                FontWeight = "500",
                LineHeight = "1.4",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            Body1 = new Body1Typography
            {
                FontFamily = fontStack,
                FontSize = "13px",
                FontWeight = "400",
                LineHeight = "1.4",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            Body2 = new Body2Typography
            {
                FontFamily = fontStack,
                FontSize = "12px",
                FontWeight = "400",
                LineHeight = "1.4",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            Button = new ButtonTypography
            {
                FontFamily = fontStack,
                FontSize = "13px",
                FontWeight = "600",
                LineHeight = "1.4",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            Caption = new CaptionTypography
            {
                FontFamily = fontStack,
                FontSize = "11px",
                FontWeight = "400",
                LineHeight = "1.3",
                LetterSpacing = "0",
                TextTransform = "none",
            },
            Overline = new OverlineTypography
            {
                FontFamily = fontStack,
                FontSize = "11px",
                FontWeight = "600",
                LineHeight = "1.3",
                LetterSpacing = "0",
                TextTransform = "none",
            },
        };

        // ═══════════════════════════════════════════════════════════════════
        //  LIGHT MODE — Layout Properties
        // ═══════════════════════════════════════════════════════════════════
        theme.LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "260px",
            DrawerMiniWidthLeft = "60px",
            DrawerMiniWidthRight = "60px",
            AppbarHeight = "64px",
        };

        // ═══════════════════════════════════════════════════════════════════
        //  LIGHT MODE — Z-Index
        // ═══════════════════════════════════════════════════════════════════
        theme.ZIndex = new ZIndex
        {
            Drawer = 1100,
            Popover = 1200,
            AppBar = 1300,
            Dialog = 1400,
            Snackbar = 1500,
            Tooltip = 1600,
        };

        // ═══════════════════════════════════════════════════════════════════
        //  DARK MODE — Palette
        // ═══════════════════════════════════════════════════════════════════
        theme.PaletteDark = new PaletteDark
        {
            // ── Core palette (MudColor) ─────────────────────────────────
            Primary = new MudColor("#fafafa"),
            PrimaryContrastText = new MudColor("#171717"),
            PrimaryLighten = "#ffffff",
            PrimaryDarken = "#ebebeb",

            Secondary = new MudColor("#a3a3a3"),
            SecondaryContrastText = new MudColor("#171717"),
            SecondaryLighten = "#2f2f2f",
            SecondaryDarken = "#d4d4d4",

            Tertiary = new MudColor("#0a72ef"),
            TertiaryContrastText = new MudColor("#ffffff"),
            TertiaryLighten = "#0b2f62",
            TertiaryDarken = "#60a5fa",

            Info = new MudColor("#60a5fa"),
            InfoContrastText = new MudColor("#171717"),
            InfoLighten = "#0b2f62",
            InfoDarken = "#93c5fd",

            Success = new MudColor("#10b981"),
            SuccessContrastText = new MudColor("#ffffff"),
            SuccessLighten = "#064e3b",
            SuccessDarken = "#34d399",

            Warning = new MudColor("#f59e0b"),
            WarningContrastText = new MudColor("#0f172a"),
            WarningLighten = "#451a03",
            WarningDarken = "#fbbf24",

            Error = new MudColor("#ef4444"),
            ErrorContrastText = new MudColor("#ffffff"),
            ErrorLighten = "#450a0a",
            ErrorDarken = "#f87171",

            Dark = new MudColor("#fafafa"),
            DarkContrastText = new MudColor("#171717"),
            DarkLighten = "#ffffff",
            DarkDarken = "#ebebeb",

            Black = new MudColor("#000000"),
            White = new MudColor("#ffffff"),

            // ── Text (MudColor) ─────────────────────────────────────────
            TextPrimary = new MudColor("#fafafa"),
            TextSecondary = new MudColor("#a3a3a3"),
            TextDisabled = new MudColor("#666666"),

            // ── Action / interaction (MudColor) ─────────────────────────
            ActionDefault = new MudColor("#a3a3a3"),
            ActionDisabled = new MudColor("#666666"),
            ActionDisabledBackground = new MudColor("#2f2f2f"),

            // ── Surfaces (MudColor) ─────────────────────────────────────
            Background = new MudColor("#0a0a0a"),
            BackgroundGray = new MudColor("#111111"),
            Surface = new MudColor("#171717"),
            DrawerBackground = new MudColor("#171717"),
            DrawerText = new MudColor("#fafafa"),
            DrawerIcon = new MudColor("#a3a3a3"),

            // ── App bar (MudColor) ──────────────────────────────────────
            AppbarBackground = new MudColor("#171717"),
            AppbarText = new MudColor("#fafafa"),

            // ── Lines / dividers (MudColor) ──────────────────────────────
            LinesDefault = new MudColor("#2f2f2f"),
            LinesInputs = new MudColor("#404040"),
            TableLines = new MudColor("#2f2f2f"),
            TableStriped = new MudColor("#111111"),
            TableHover = new MudColor("#202020"),
            Divider = new MudColor("#2f2f2f"),
            DividerLight = new MudColor("#202020"),

            // ── Misc (MudColor) ─────────────────────────────────────────
            Skeleton = new MudColor("#2f2f2f"),

            // ── Gray / overlay scale ───────────────────────────────────
            GrayDefault = "#808080",
            GrayLight = "#4d4d4d",
            GrayLighter = "#2f2f2f",
            GrayDark = "#d4d4d4",
            GrayDarker = "#fafafa",
            OverlayDark = "rgba(0,0,0,0.7)",
            OverlayLight = "rgba(255,255,255,0.1)",

            // ── Opacity / ripple (double) ──────────────────────────────
            BorderOpacity = 1.0,
            HoverOpacity = 0.08,
            RippleOpacity = 0.0,
            RippleOpacitySecondary = 0.0,
        };

        // ═══════════════════════════════════════════════════════════════════
        //  DARK MODE — Typography
        //  Reuses the same Typography instance as Light — only palette differs.
        // ═══════════════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════════════
        //  DARK MODE — Layout Properties
        //  Reuses the same LayoutProperties instance as Light.
        // ═══════════════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════════════
        //  DARK MODE — Z-Index
        //  Reuses the same ZIndex instance as Light.
        // ═══════════════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════════════
        //  SHARED — Shadows  (MudBlazor built-in Material elevation scale)
        // ═══════════════════════════════════════════════════════════════════
        theme.Shadows = new Shadow
        {
            Elevation =
            [
                "none",
                "rgba(0,0,0,0.08) 0 0 0 1px",
                "rgba(0,0,0,0.08) 0 0 0 1px, rgba(0,0,0,0.04) 0 2px 2px",
                "rgba(0,0,0,0.08) 0 0 0 1px, rgba(0,0,0,0.04) 0 2px 2px, rgba(0,0,0,0.04) 0 8px 8px -8px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.05) 0 4px 10px -6px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.06) 0 8px 18px -12px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.07) 0 12px 28px -18px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.07) 0 14px 34px -20px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.08) 0 16px 40px -24px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.08) 0 18px 44px -26px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.09) 0 20px 48px -28px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.09) 0 22px 52px -30px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.10) 0 24px 56px -32px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.10) 0 26px 60px -34px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.10) 0 28px 64px -36px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.10) 0 30px 68px -38px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.11) 0 32px 72px -40px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.11) 0 34px 76px -42px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.11) 0 36px 80px -44px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.12) 0 38px 84px -46px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.12) 0 40px 88px -48px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.12) 0 42px 92px -50px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.12) 0 44px 96px -52px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.13) 0 46px 100px -54px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.13) 0 48px 104px -56px",
                "rgba(0,0,0,0.10) 0 0 0 1px, rgba(0,0,0,0.13) 0 50px 108px -58px",
            ],
        };

        // ═══════════════════════════════════════════════════════════════════
        //  SHARED — PseudoCss  (CSS variable injection scope)
        // ═══════════════════════════════════════════════════════════════════
        theme.PseudoCss = new PseudoCss { Scope = ":root" };

        return theme;
    }
}

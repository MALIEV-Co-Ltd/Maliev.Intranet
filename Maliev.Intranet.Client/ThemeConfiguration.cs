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

        string[] fontStack = ["Noto Sans", "Noto Sans Thai", "sans-serif"];

        // ═══════════════════════════════════════════════════════════════════
        //  LIGHT MODE — Palette
        // ═══════════════════════════════════════════════════════════════════
        theme.PaletteLight = new PaletteLight
        {
            // ── Core palette (MudColor) ─────────────────────────────────
            Primary = new MudColor("#2563eb"),
            PrimaryContrastText = new MudColor("#ffffff"),
            PrimaryLighten = "#e0e7ff",
            PrimaryDarken = "#1d4ed8",

            Secondary = new MudColor("#64748b"),
            SecondaryContrastText = new MudColor("#ffffff"),
            SecondaryLighten = "#f1f5f9",
            SecondaryDarken = "#475569",

            Tertiary = new MudColor("#7c3aed"),
            TertiaryContrastText = new MudColor("#ffffff"),
            TertiaryLighten = "#ede9fe",
            TertiaryDarken = "#6d28d9",

            Info = new MudColor("#0284c7"),
            InfoContrastText = new MudColor("#ffffff"),
            InfoLighten = "#e0f2fe",
            InfoDarken = "#0369a1",

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
            TextPrimary = new MudColor("#1a1a1a"),
            TextSecondary = new MudColor("#6b7280"),
            TextDisabled = new MudColor("#9ca3af"),

            // ── Action / interaction (MudColor) ─────────────────────────
            ActionDefault = new MudColor("#64748b"),
            ActionDisabled = new MudColor("#d1d5db"),
            ActionDisabledBackground = new MudColor("#e5e7eb"),

            // ── Surfaces (MudColor) ─────────────────────────────────────
            Background = new MudColor("#f1f5f9"),
            BackgroundGray = new MudColor("#f8fafc"),
            Surface = new MudColor("#ffffff"),
            DrawerBackground = new MudColor("#ffffff"),
            DrawerText = new MudColor("#1a1a1a"),
            DrawerIcon = new MudColor("#64748b"),

            // ── App bar (MudColor) ──────────────────────────────────────
            AppbarBackground = new MudColor("#ffffff"),
            AppbarText = new MudColor("#1a1a1a"),

            // ── Lines / dividers (MudColor) ──────────────────────────────
            LinesDefault = new MudColor("#e4e4e7"),
            LinesInputs = new MudColor("#d4d4d8"),
            TableLines = new MudColor("#e4e4e7"),
            TableStriped = new MudColor("#f9fafb"),
            TableHover = new MudColor("#f3f4f6"),
            Divider = new MudColor("#e4e4e7"),
            DividerLight = new MudColor("#f3f4f6"),

            // ── Misc (MudColor) ─────────────────────────────────────────
            Skeleton = new MudColor("#e5e7eb"),

            // ── Gray / overlay scale ───────────────────────────────────
            GrayDefault = "#94a3b8",
            GrayLight = "#cbd5e1",
            GrayLighter = "#e2e8f0",
            GrayDark = "#475569",
            GrayDarker = "#334155",
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
                FontWeight = "700",
                LineHeight = "1.2",
                LetterSpacing = "-0.5px",
                TextTransform = "none",
            },
            H2 = new H2Typography
            {
                FontFamily = fontStack,
                FontSize = "22px",
                FontWeight = "700",
                LineHeight = "1.25",
                LetterSpacing = "-0.25px",
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
                LetterSpacing = "0.5px",
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
            Primary = new MudColor("#3b82f6"),
            PrimaryContrastText = new MudColor("#ffffff"),
            PrimaryLighten = "#1e3a5f",
            PrimaryDarken = "#60a5fa",

            Secondary = new MudColor("#94a3b8"),
            SecondaryContrastText = new MudColor("#0f172a"),
            SecondaryLighten = "#1e293b",
            SecondaryDarken = "#cbd5e1",

            Tertiary = new MudColor("#a78bfa"),
            TertiaryContrastText = new MudColor("#0f172a"),
            TertiaryLighten = "#2e1065",
            TertiaryDarken = "#c4b5fd",

            Info = new MudColor("#38bdf8"),
            InfoContrastText = new MudColor("#0f172a"),
            InfoLighten = "#0c4a6e",
            InfoDarken = "#7dd3fc",

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

            Dark = new MudColor("#f1f5f9"),
            DarkContrastText = new MudColor("#0f172a"),
            DarkLighten = "#e2e8f0",
            DarkDarken = "#cbd5e1",

            Black = new MudColor("#000000"),
            White = new MudColor("#ffffff"),

            // ── Text (MudColor) ─────────────────────────────────────────
            TextPrimary = new MudColor("#f1f5f9"),
            TextSecondary = new MudColor("#94a3b8"),
            TextDisabled = new MudColor("#475569"),

            // ── Action / interaction (MudColor) ─────────────────────────
            ActionDefault = new MudColor("#94a3b8"),
            ActionDisabled = new MudColor("#334155"),
            ActionDisabledBackground = new MudColor("#1e293b"),

            // ── Surfaces (MudColor) ─────────────────────────────────────
            Background = new MudColor("#131620"),
            BackgroundGray = new MudColor("#1e2130"),
            Surface = new MudColor("#1e2130"),
            DrawerBackground = new MudColor("#1e2130"),
            DrawerText = new MudColor("#f1f5f9"),
            DrawerIcon = new MudColor("#94a3b8"),

            // ── App bar (MudColor) ──────────────────────────────────────
            AppbarBackground = new MudColor("#1e2130"),
            AppbarText = new MudColor("#f1f5f9"),

            // ── Lines / dividers (MudColor) ──────────────────────────────
            LinesDefault = new MudColor("#2d3148"),
            LinesInputs = new MudColor("#3d4160"),
            TableLines = new MudColor("#2d3148"),
            TableStriped = new MudColor("#252a3d"),
            TableHover = new MudColor("#2d3148"),
            Divider = new MudColor("#2d3148"),
            DividerLight = new MudColor("#1e2130"),

            // ── Misc (MudColor) ─────────────────────────────────────────
            Skeleton = new MudColor("#252a3d"),

            // ── Gray / overlay scale ───────────────────────────────────
            GrayDefault = "#64748b",
            GrayLight = "#475569",
            GrayLighter = "#334155",
            GrayDark = "#cbd5e1",
            GrayDarker = "#e2e8f0",
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
                "0px 2px 1px -1px rgba(0,0,0,0.2),0px 1px 1px 0px rgba(0,0,0,0.14),0px 1px 3px 0px rgba(0,0,0,0.12)",
                "0px 3px 1px -2px rgba(0,0,0,0.2),0px 2px 2px 0px rgba(0,0,0,0.14),0px 1px 5px 0px rgba(0,0,0,0.12)",
                "0px 3px 3px -2px rgba(0,0,0,0.2),0px 3px 4px 0px rgba(0,0,0,0.14),0px 1px 8px 0px rgba(0,0,0,0.12)",
                "0px 2px 4px -1px rgba(0,0,0,0.2),0px 4px 5px 0px rgba(0,0,0,0.14),0px 1px 10px 0px rgba(0,0,0,0.12)",
                "0px 3px 5px -1px rgba(0,0,0,0.2),0px 5px 8px 0px rgba(0,0,0,0.14),0px 1px 14px 0px rgba(0,0,0,0.12)",
                "0px 3px 5px -1px rgba(0,0,0,0.2),0px 6px 10px 0px rgba(0,0,0,0.14),0px 1px 18px 0px rgba(0,0,0,0.12)",
                "0px 4px 5px -2px rgba(0,0,0,0.2),0px 7px 10px 1px rgba(0,0,0,0.14),0px 2px 16px 1px rgba(0,0,0,0.12)",
                "0px 5px 5px -3px rgba(0,0,0,0.2),0px 8px 10px 1px rgba(0,0,0,0.14),0px 3px 14px 2px rgba(0,0,0,0.12)",
                "0px 5px 6px -3px rgba(0,0,0,0.2),0px 9px 12px 1px rgba(0,0,0,0.14),0px 3px 16px 2px rgba(0,0,0,0.12)",
                "0px 6px 6px -3px rgba(0,0,0,0.2),0px 10px 14px 1px rgba(0,0,0,0.14),0px 4px 18px 3px rgba(0,0,0,0.12)",
                "0px 6px 7px -4px rgba(0,0,0,0.2),0px 11px 15px 1px rgba(0,0,0,0.14),0px 4px 20px 3px rgba(0,0,0,0.12)",
                "0px 7px 8px -4px rgba(0,0,0,0.2),0px 12px 17px 2px rgba(0,0,0,0.14),0px 5px 22px 4px rgba(0,0,0,0.12)",
                "0px 7px 8px -4px rgba(0,0,0,0.2),0px 13px 19px 2px rgba(0,0,0,0.14),0px 5px 24px 4px rgba(0,0,0,0.12)",
                "0px 7px 9px -4px rgba(0,0,0,0.2),0px 14px 21px 2px rgba(0,0,0,0.14),0px 5px 26px 4px rgba(0,0,0,0.12)",
                "0px 8px 9px -5px rgba(0,0,0,0.2),0px 15px 22px 2px rgba(0,0,0,0.14),0px 6px 28px 5px rgba(0,0,0,0.12)",
                "0px 8px 10px -5px rgba(0,0,0,0.2),0px 16px 24px 2px rgba(0,0,0,0.14),0px 6px 30px 5px rgba(0,0,0,0.12)",
                "0px 8px 11px -5px rgba(0,0,0,0.2),0px 17px 26px 2px rgba(0,0,0,0.14),0px 6px 32px 5px rgba(0,0,0,0.12)",
                "0px 9px 11px -5px rgba(0,0,0,0.2),0px 18px 28px 2px rgba(0,0,0,0.14),0px 7px 34px 6px rgba(0,0,0,0.12)",
                "0px 9px 12px -6px rgba(0,0,0,0.2),0px 19px 29px 2px rgba(0,0,0,0.14),0px 7px 36px 6px rgba(0,0,0,0.12)",
                "0px 10px 13px -6px rgba(0,0,0,0.2),0px 20px 31px 3px rgba(0,0,0,0.14),0px 8px 38px 7px rgba(0,0,0,0.12)",
                "0px 10px 13px -6px rgba(0,0,0,0.2),0px 21px 33px 3px rgba(0,0,0,0.14),0px 8px 40px 7px rgba(0,0,0,0.12)",
                "0px 10px 14px -6px rgba(0,0,0,0.2),0px 22px 35px 3px rgba(0,0,0,0.14),0px 8px 42px 7px rgba(0,0,0,0.12)",
                "0px 11px 14px -7px rgba(0,0,0,0.2),0px 23px 36px 3px rgba(0,0,0,0.14),0px 9px 44px 8px rgba(0,0,0,0.12)",
                "0px 11px 15px -7px rgba(0,0,0,0.2),0px 24px 38px 3px rgba(0,0,0,0.14),0px 9px 46px 8px rgba(0,0,0,0.12)",
                "0 5px 5px -3px rgba(0,0,0,.06), 0 8px 10px 1px rgba(0,0,0,.042), 0 3px 14px 2px rgba(0,0,0,.036)",
            ],
        };

        // ═══════════════════════════════════════════════════════════════════
        //  SHARED — PseudoCss  (CSS variable injection scope)
        // ═══════════════════════════════════════════════════════════════════
        theme.PseudoCss = new PseudoCss { Scope = ":root" };

        return theme;
    }
}

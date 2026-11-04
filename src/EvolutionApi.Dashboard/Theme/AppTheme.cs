using MudBlazor;

namespace EvolutionApi.Dashboard.Theme;

/// <summary>
/// Premium MudBlazor theme for the WhatsApp AI Chatbot Management Platform.
/// Applies glassmorphism, custom palette, and Inter-compatible typography.
/// </summary>
public static class AppTheme
{
    public static MudTheme DarkTheme { get; } = BuildDark();
    public static MudTheme LightTheme { get; } = BuildLight();

    private static MudTheme BuildDark() => new()
    {
        PaletteLight = new PaletteLight
        {
            // Not used in dark mode, but set sensible defaults
            Primary = "#25D366",
            PrimaryDarken = "#128C7E",
            PrimaryLighten = "#66BB6A",
        },
        PaletteDark = new PaletteDark
        {
            Primary          = "#25D366",
            PrimaryDarken    = "#128C7E",
            PrimaryLighten   = "#66BB6A",
            PrimaryContrastText = "#000000",

            Secondary        = "#7C3AED",
            SecondaryDarken  = "#5B21B6",
            SecondaryLighten = "#A78BFA",

            Tertiary         = "#06B6D4",
            TertiaryDarken   = "#0E7490",
            TertiaryLighten  = "#67E8F9",

            Info             = "#3B82F6",
            InfoDarken       = "#1D4ED8",
            InfoLighten      = "#93C5FD",

            Warning          = "#F59E0B",
            WarningDarken    = "#B45309",
            WarningLighten   = "#FCD34D",

            Error            = "#F43F5E",
            ErrorDarken      = "#BE123C",
            ErrorLighten     = "#FDA4AF",

            Success          = "#22C55E",
            SuccessDarken    = "#15803D",
            SuccessLighten   = "#86EFAC",

            Background           = "#0A0E1A",
            BackgroundGray       = "#111827",
            Surface              = "#1C2534",
            DrawerBackground     = "#111827",
            DrawerText           = "rgba(255,255,255,0.87)",
            DrawerIcon           = "rgba(255,255,255,0.65)",
            AppbarBackground     = "rgba(10,14,26,0.85)",
            AppbarText           = "rgba(255,255,255,0.90)",

            TextPrimary          = "rgba(255,255,255,0.95)",
            TextSecondary        = "rgba(255,255,255,0.65)",
            TextDisabled         = "rgba(255,255,255,0.35)",

            ActionDefault        = "rgba(255,255,255,0.65)",
            ActionDisabled       = "rgba(255,255,255,0.25)",
            ActionDisabledBackground = "rgba(255,255,255,0.08)",

            LinesDefault         = "rgba(255,255,255,0.08)",
            LinesInputs          = "rgba(255,255,255,0.14)",
            TableLines           = "rgba(255,255,255,0.06)",

            Divider              = "rgba(255,255,255,0.08)",
            DividerLight         = "rgba(255,255,255,0.05)",

            HoverOpacity         = 0.06,
            RippleOpacity        = 0.12,
            RippleOpacitySecondary = 0.08,
            GrayDefault          = "#243040",
            GrayLight            = "#1C2534",
            GrayDark             = "#0A0E1A",

            OverlayDark          = "rgba(0,0,0,0.70)",
            OverlayLight         = "rgba(255,255,255,0.08)",

            Dark                 = "#1C2534",
            DarkContrastText     = "rgba(255,255,255,0.87)",
            DarkDarken           = "#111827",
            DarkLighten          = "#243040",

            White                = "#FFFFFF",
            Black                = "#000000",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius  = "12px",
            DrawerWidthLeft      = "260px",
            DrawerMiniWidthLeft  = "72px",
            DrawerWidthRight     = "280px",
            AppbarHeight         = "64px",
        },
        Typography = BuildTypography(),
        ZIndex = new ZIndex
        {
            Drawer    = 1200,
            AppBar    = 1100,
            Dialog    = 1300,
            Popover   = 1400,
            Snackbar  = 1500,
            Tooltip   = 1600,
        },
    };

    private static MudTheme BuildLight() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary          = "#25D366",
            PrimaryDarken    = "#128C7E",
            PrimaryLighten   = "#66BB6A",
            PrimaryContrastText = "#FFFFFF",

            Secondary        = "#7C3AED",
            Tertiary         = "#06B6D4",
            Info             = "#3B82F6",
            Warning          = "#F59E0B",
            Error            = "#F43F5E",
            Success          = "#22C55E",

            Background       = "#F0F4F8",
            BackgroundGray   = "#E9EEF5",
            Surface          = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            AppbarBackground = "rgba(240,244,248,0.90)",

            TextPrimary      = "rgba(0,0,0,0.87)",
            TextSecondary    = "rgba(0,0,0,0.60)",
            TextDisabled     = "rgba(0,0,0,0.38)",

            LinesDefault     = "rgba(0,0,0,0.08)",
            TableLines       = "rgba(0,0,0,0.06)",
            Divider          = "rgba(0,0,0,0.08)",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius  = "12px",
            DrawerWidthLeft      = "260px",
            DrawerMiniWidthLeft  = "72px",
            AppbarHeight         = "64px",
        },
        Typography = BuildTypography(),
    };

    private static Typography BuildTypography() => new()
    {
        Default = new DefaultTypography
        {
            FontFamily = ["Plus Jakarta Sans", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
            FontSize   = "0.875rem",
            FontWeight = "400",
            LineHeight = "1.5",
            LetterSpacing = "0em",
        },
        H1 = new H1Typography { FontSize = "2.25rem", FontWeight = "700", LineHeight = "1.2", LetterSpacing = "-0.02em" },
        H2 = new H2Typography { FontSize = "1.875rem", FontWeight = "700", LineHeight = "1.25", LetterSpacing = "-0.015em" },
        H3 = new H3Typography { FontSize = "1.5rem", FontWeight = "600", LineHeight = "1.3", LetterSpacing = "-0.01em" },
        H4 = new H4Typography { FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.35", LetterSpacing = "-0.005em" },
        H5 = new H5Typography { FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.4" },
        H6 = new H6Typography { FontSize = "1rem", FontWeight = "600", LineHeight = "1.4" },
        Subtitle1 = new Subtitle1Typography { FontSize = "1rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = "0.01em" },
        Subtitle2 = new Subtitle2Typography { FontSize = "0.875rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = "0.01em" },
        Body1 = new Body1Typography { FontSize = "1rem", FontWeight = "400", LineHeight = "1.6" },
        Body2 = new Body2Typography { FontSize = "0.875rem", FontWeight = "400", LineHeight = "1.6" },
        Button = new ButtonTypography { FontSize = "0.875rem", FontWeight = "600", LineHeight = "1", LetterSpacing = "0.02em", TextTransform = "none" },
        Caption = new CaptionTypography { FontSize = "0.75rem", FontWeight = "400", LineHeight = "1.4", LetterSpacing = "0.03em" },
        Overline = new OverlineTypography { FontSize = "0.75rem", FontWeight = "600", LineHeight = "1", LetterSpacing = "0.08em", TextTransform = "uppercase" },
    };

    private static Shadow BuildShadows() => new()
    {
        Elevation = [
            "none",
            "0 1px 3px rgba(0,0,0,0.3), 0 1px 2px rgba(0,0,0,0.2)",
            "0 2px 6px rgba(0,0,0,0.35), 0 1px 3px rgba(0,0,0,0.25)",
            "0 4px 12px rgba(0,0,0,0.4), 0 2px 6px rgba(0,0,0,0.3)",
            "0 6px 18px rgba(0,0,0,0.42), 0 3px 8px rgba(0,0,0,0.3)",
            "0 8px 24px rgba(0,0,0,0.44), 0 4px 10px rgba(0,0,0,0.3)",
            "0 10px 30px rgba(0,0,0,0.5), 0 4px 12px rgba(0,0,0,0.3)",
            "0 12px 36px rgba(0,0,0,0.5), 0 6px 14px rgba(0,0,0,0.3)",
            "0 14px 42px rgba(0,0,0,0.52), 0 6px 16px rgba(0,0,0,0.3)",
            "0 16px 48px rgba(0,0,0,0.54), 0 8px 18px rgba(0,0,0,0.3)",
            "0 18px 54px rgba(0,0,0,0.56), 0 8px 20px rgba(0,0,0,0.3)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
            "0 20px 60px rgba(0,0,0,0.6), 0 8px 24px rgba(0,0,0,0.4)",
        ],
    };
}


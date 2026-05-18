using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Specifies the display theme mode preference for the application.
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Follow the operating system or browser color scheme preference.
    /// </summary>
    System,

    /// <summary>
    /// Always use the light theme regardless of system preferences.
    /// </summary>
    Light,

    /// <summary>
    /// Always use the dark theme regardless of system preferences.
    /// </summary>
    Dark
}

/// <summary>
/// Service to manage the application layout and theme state with zero-flash persistence.
/// Uses blocking script initialization and system preference detection.
/// </summary>
public class LayoutService : IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LayoutService> _logger;
    private bool _isDarkMode;
    private ThemeMode _currentMode = ThemeMode.System;
    private bool _isInitialized;
    private bool _systemPreferencesIsDark;
    private string _language = "en-TH";
    private string _dateFormat = "dd MMM yyyy";
    private string _timeZone = "Asia/Bangkok";
    private bool _compactWorkspace;
    private const string ThemeCookieName = "maliev_theme";

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JS runtime for theme persistence.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <param name="httpContextAccessor">Optional HTTP context accessor for reading theme cookies during server-side rendering (SSR).</param>
    public LayoutService(
        IJSRuntime jsRuntime,
        ILogger<LayoutService> logger,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;

        // CRITICAL FIX: Read theme from cookie during SSR to prevent flash
        // This ensures the initial render has the correct theme
        var context = httpContextAccessor?.HttpContext;
        if (context != null)
        {
            var cookieValue = context.Request.Cookies[ThemeCookieName];
            var systemDarkHint = context.Request.Cookies["maliev_system_dark"];

            if (!string.IsNullOrEmpty(cookieValue))
            {
                _currentMode = cookieValue switch
                {
                    "dark" => ThemeMode.Dark,
                    "light" => ThemeMode.Light,
                    _ => ThemeMode.System
                };
            }

            if (bool.TryParse(systemDarkHint, out var isDark))
            {
                _systemPreferencesIsDark = isDark;
            }

            CalculateEffectiveTheme();
        }
        else
        {
            // Client-side rendering (WASM) - will initialize in OnAfterRenderAsync
            _currentMode = ThemeMode.System;
            _isDarkMode = false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether dark mode is currently active (effective state).
    /// READ-ONLY property to prevent two-way binding issues.
    /// </summary>
    public bool IsDarkMode => _isDarkMode;

    /// <summary>
    /// Gets the current user preference mode.
    /// </summary>
    public ThemeMode CurrentMode => _currentMode;

    /// <summary>
    /// Gets the currently selected employee language preference.
    /// </summary>
    public string Language => _language;

    /// <summary>
    /// Gets the currently selected employee date display format.
    /// </summary>
    public string DateFormat => _dateFormat;

    /// <summary>
    /// Gets the currently selected employee time zone preference.
    /// </summary>
    public string TimeZone => _timeZone;

    /// <summary>
    /// Gets a value indicating whether compact workspace density is enabled.
    /// </summary>
    public bool CompactWorkspace => _compactWorkspace;

    /// <summary>
    /// Initializes the theme service by reading from DOM.
    /// This should be called in OnAfterRenderAsync after JS interop is available.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            _logger.LogDebug("Theme already initialized, skipping");
            return;
        }

        try
        {
            // CRITICAL: Read from data-maliev-theme attribute first (set by blocking script)
            // This ensures we sync with what the user sees on initial render
            var currentThemeAttr = await _jsRuntime.InvokeAsync<string>(
                "eval",
                "document.documentElement.getAttribute('data-maliev-theme') || 'light'"
            );

            // Read user preference from cookie/storage
            var themePref = await _jsRuntime.InvokeAsync<string>(
                "eval",
                $"document.cookie.split('; ').find(row => row.startsWith('{ThemeCookieName}='))?.split('=')[1] || localStorage.getItem('{ThemeCookieName}') || 'system'"
            );

            _currentMode = themePref switch
            {
                "dark" => ThemeMode.Dark,
                "light" => ThemeMode.Light,
                _ => ThemeMode.System
            };

            // Also check current system state for "Auto" mode
            _systemPreferencesIsDark = await _jsRuntime.InvokeAsync<bool>(
                "eval",
                "window.matchMedia('(prefers-color-scheme: dark)').matches"
            );

            // Calculate what theme should be active based on preference
            CalculateEffectiveTheme();

            // Trust the blocking script's decision (it ran first and set data-maliev-theme)
            // This prevents any flash
            var expectedTheme = _isDarkMode ? "dark" : "light";
            if (currentThemeAttr != expectedTheme)
            {
                _isDarkMode = currentThemeAttr == "dark";
            }

            _isInitialized = true;

            // Notify UI to update
            MajorUpdateOccurred?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            // Ignore JS interop errors during prerendering
            if (ex.GetType().Name == "JSDisconnectedException" ||
                ex.Message.Contains("JavaScript interop calls cannot be issued at this time"))
            {
                _logger.LogDebug("JS not available yet (prerendering)");
                return;
            }

            _logger.LogError(ex, "Failed to initialize theme");
            // Default to system/light on error
            _currentMode = ThemeMode.System;
            _isDarkMode = false;
            _isInitialized = true;
        }

    }

    /// <summary>
    /// Toggles the theme mode in a cycle: System -> Light -> Dark -> System.
    /// </summary>
    public async Task ToggleModeAsync()
    {
        var newMode = _currentMode switch
        {
            ThemeMode.System => ThemeMode.Light,
            ThemeMode.Light => ThemeMode.Dark,
            ThemeMode.Dark => ThemeMode.System,
            _ => ThemeMode.System
        };

        await SetModeAsync(newMode);
    }

    /// <summary>
    /// Sets the theme mode explicitly and persists to storage.
    /// </summary>
    /// <param name="mode">The desired theme mode.</param>
    public async Task SetModeAsync(ThemeMode mode)
    {
        if (_currentMode == mode)
        {
            return; // No change
        }

        _currentMode = mode;
        CalculateEffectiveTheme(); // Update local state immediately

        var themeString = mode switch
        {
            ThemeMode.Dark => "dark",
            ThemeMode.Light => "light",
            _ => "system"
        };

        try
        {
            // Calculate what the DOM attribute should be
            var effectiveTheme = _isDarkMode ? "dark" : "light";

            // Update DOM immediately for responsiveness
            await _jsRuntime.InvokeVoidAsync(
                "eval",
                $"document.documentElement.setAttribute('data-maliev-theme', '{effectiveTheme}')"
            );

            // Persist preference to cookie
            await _jsRuntime.InvokeVoidAsync(
                "eval",
                $"document.cookie = '{ThemeCookieName}={themeString}; path=/; max-age=31536000; SameSite=Lax'"
            );

            // Persist to localStorage (fallback)
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeCookieName, themeString);

            MajorUpdateOccurred?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set theme");
        }
    }

    /// <summary>
    /// Applies saved employee workspace preferences to the current client session.
    /// </summary>
    /// <param name="themeMode">The saved theme mode.</param>
    /// <param name="language">The saved UI language.</param>
    /// <param name="dateFormat">The saved date display format.</param>
    /// <param name="timeZone">The saved time zone identifier.</param>
    /// <param name="compactWorkspace">Whether compact workspace density is enabled.</param>
    public async Task ApplyWorkspacePreferencesAsync(
        string? themeMode,
        string? language,
        string? dateFormat,
        string? timeZone,
        bool? compactWorkspace)
    {
        await SetModeAsync(ParseThemeMode(themeMode));

        _language = NormalizeAllowedValue(language, ["en-TH", "th-TH", "en-US"], "en-TH");
        _dateFormat = NormalizeAllowedValue(dateFormat, ["dd MMM yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "MMM d, yyyy"], "dd MMM yyyy");
        _timeZone = NormalizeAllowedValue(timeZone, ["Asia/Bangkok", "UTC", "Asia/Singapore", "Asia/Tokyo", "Europe/Berlin", "America/Los_Angeles"], "Asia/Bangkok");
        _compactWorkspace = compactWorkspace ?? false;

        ApplyCulture(_language);
        await ApplyPreferenceAttributesAsync();
        MajorUpdateOccurred?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Formats a date using the current employee date format preference.
    /// </summary>
    /// <param name="value">The date to format.</param>
    /// <returns>The formatted date or a dash when no value is supplied.</returns>
    public string FormatDate(DateTime? value) =>
        value.HasValue ? value.Value.ToString(_dateFormat, CultureInfo.CurrentCulture) : "-";

    /// <summary>
    /// Formats a date and time using the current employee date format preference.
    /// </summary>
    /// <param name="value">The date and time to format.</param>
    /// <returns>The formatted date and time or a dash when no value is supplied.</returns>
    public string FormatDateTime(DateTime? value) =>
        value.HasValue ? $"{FormatDate(value)} {value.Value.ToString("HH:mm", CultureInfo.CurrentCulture)}" : "-";

    /// <summary>
    /// Updates the system preference state (called by MudThemeProvider watcher).
    /// </summary>
    /// <param name="isSystemDark">Whether the system is currently in dark mode.</param>
    public void UpdateSystemPreference(bool isSystemDark)
    {
        if (_systemPreferencesIsDark != isSystemDark)
        {
            _systemPreferencesIsDark = isSystemDark;
            if (_currentMode == ThemeMode.System)
            {
                CalculateEffectiveTheme();
                MajorUpdateOccurred?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void CalculateEffectiveTheme()
    {
        _isDarkMode = _currentMode switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            ThemeMode.System => _systemPreferencesIsDark,
            _ => false
        };
    }

    private static ThemeMode ParseThemeMode(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "dark" => ThemeMode.Dark,
            "light" => ThemeMode.Light,
            _ => ThemeMode.System
        };

    private static string NormalizeAllowedValue(string? value, IReadOnlyList<string> allowedValues, string fallback)
    {
        return allowedValues.FirstOrDefault(candidate =>
            string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase)) ?? fallback;
    }

    private static void ApplyCulture(string language)
    {
        try
        {
            var culture = (CultureInfo)CultureInfo.GetCultureInfo(language).Clone();
            culture.DateTimeFormat.Calendar = new GregorianCalendar();
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch (CultureNotFoundException)
        {
            var fallback = (CultureInfo)CultureInfo.GetCultureInfo("en-TH").Clone();
            fallback.DateTimeFormat.Calendar = new GregorianCalendar();
            CultureInfo.DefaultThreadCurrentCulture = fallback;
            CultureInfo.DefaultThreadCurrentUICulture = fallback;
        }
    }

    private async Task ApplyPreferenceAttributesAsync()
    {
        try
        {
            var language = JsonSerializer.Serialize(_language);
            var dateFormat = JsonSerializer.Serialize(_dateFormat);
            var timeZone = JsonSerializer.Serialize(_timeZone);
            var compactWorkspace = JsonSerializer.Serialize(_compactWorkspace ? "true" : "false");

            await _jsRuntime.InvokeVoidAsync(
                "eval",
                $"""
                document.documentElement.lang = {language};
                document.documentElement.setAttribute('data-maliev-date-format', {dateFormat});
                document.documentElement.setAttribute('data-maliev-time-zone', {timeZone});
                document.documentElement.setAttribute('data-maliev-compact-workspace', {compactWorkspace});
                """);
        }
        catch (Exception ex)
        {
            if (ex.GetType().Name == "JSDisconnectedException" ||
                ex.Message.Contains("JavaScript interop calls cannot be issued at this time"))
            {
                _logger.LogDebug("JS not available yet (prerendering)");
                return;
            }

            _logger.LogError(ex, "Failed to apply workspace preference attributes");
        }
    }

    /// <summary>
    /// Event triggered when a major layout update occurs (e.g., theme toggle).
    /// </summary>
    public event EventHandler? MajorUpdateOccurred;

    /// <summary>
    /// Disposes the service and cleanup resources.
    /// </summary>
    public void Dispose()
    {
        // No resources to dispose currently
    }
}

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

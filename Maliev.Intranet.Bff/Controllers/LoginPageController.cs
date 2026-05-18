using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Serves the BFF-owned employee login page without booting the WebAssembly app.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public sealed class LoginPageController : Controller
{
    private const string InvalidCredentialsMessage = "Invalid credentials. Please try again.";
    private const string TemporarySignInMessage = "We couldn't complete sign-in right now. Please try again in a moment.";

    /// <summary>
    /// Renders the server-owned login page.
    /// </summary>
    /// <param name="returnUrl">The local app route to continue to after authentication.</param>
    /// <param name="error">Optional login error text.</param>
    /// <returns>A static HTML login page.</returns>
    [HttpGet("/login")]
    public IActionResult Login([FromQuery] string? returnUrl = "/", [FromQuery] string? error = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(ToSafeLocalUrl(returnUrl));
        }

        return Content(RenderLoginPage(ToSafeLocalUrl(returnUrl), error), "text/html; charset=utf-8");
    }

    private static string ToSafeLocalUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        return returnUrl.StartsWith("/", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("//", StringComparison.Ordinal) &&
            !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            ? returnUrl
            : "/";
    }

    private static string RenderLoginPage(string returnUrl, string? error)
    {
        var encodedReturnUrl = WebUtility.HtmlEncode(returnUrl);
        var encodedGoogleReturnUrl = WebUtility.UrlEncode(returnUrl);
        var loginError = ToUserFacingLoginError(error);
        var encodedError = WebUtility.HtmlEncode(loginError);
        var errorHtml = string.IsNullOrWhiteSpace(loginError)
            ? string.Empty
            : $"""<div class="error-alert">{encodedError}</div>""";

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>MALIEV | Intranet Gateway</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Geist:wght@400..700&family=Geist+Mono:wght@400..600&family=Noto+Sans+Thai:wght@100..900&display=swap" rel="stylesheet">
    <style>
        :root {
            color-scheme: light;
            --maliev-bg: #ffffff;
            --maliev-panel: #ffffff;
            --maliev-panel-3: #f5f5f5;
            --maliev-ink: #171717;
            --maliev-ink-2: #4d4d4d;
            --maliev-muted: #666666;
            --maliev-muted-2: #808080;
            --maliev-border: #ebebeb;
            --maliev-danger: #c1121f;
            --maliev-danger-bg: #fff0f0;
            --maliev-shadow-ring: rgba(0,0,0,0.08) 0 0 0 1px;
            --maliev-shadow-md: rgba(0,0,0,0.08) 0 0 0 1px, rgba(0,0,0,0.04) 0 2px 6px;
            --maliev-shadow-card: rgba(0,0,0,0.08) 0 0 0 1px, rgba(0,0,0,0.04) 0 2px 2px, rgba(0,0,0,0.04) 0 8px 8px -8px, #fafafa 0 0 0 1px inset;
            --maliev-focus-color: hsla(212, 100%, 48%, 1);
            --maliev-focus-ring: 0 0 0 2px #ffffff, 0 0 0 4px var(--maliev-focus-color);
            --maliev-radius-xs: 3px;
            --maliev-radius-sm: 6px;
            --maliev-radius-md: 8px;
            --maliev-font-sans: 'Geist', 'Noto Sans Thai', sans-serif;
            --maliev-font-mono: 'Geist Mono', 'Noto Sans Thai', ui-monospace, SFMono-Regular, Menlo, Monaco, 'Courier New', monospace;
            --maliev-type-h1: 28px;
            --maliev-type-body: 13px;
            --maliev-type-caption: 12px;
            --maliev-type-micro: 11px;
        }
        :root[data-maliev-theme="dark"] {
            color-scheme: dark;
            --maliev-bg: #0a0a0a;
            --maliev-panel: #171717;
            --maliev-panel-3: #202020;
            --maliev-ink: #fafafa;
            --maliev-ink-2: #a3a3a3;
            --maliev-muted: #808080;
            --maliev-muted-2: #666666;
            --maliev-border: #2f2f2f;
            --maliev-danger: #ff5b5b;
            --maliev-danger-bg: #3a1111;
            --maliev-shadow-ring: rgba(255,255,255,0.12) 0 0 0 1px;
            --maliev-shadow-md: rgba(255,255,255,0.12) 0 0 0 1px, rgba(0,0,0,0.32) 0 2px 8px;
            --maliev-shadow-card: rgba(255,255,255,0.12) 0 0 0 1px, rgba(0,0,0,0.36) 0 2px 2px, rgba(0,0,0,0.36) 0 8px 8px -8px, rgba(255,255,255,0.06) 0 0 0 1px inset;
            --maliev-focus-ring: 0 0 0 2px #0a0a0a, 0 0 0 4px var(--maliev-focus-color);
        }
        * {
            box-sizing: border-box;
        }
        body {
            margin: 0;
            min-height: 100vh;
            min-height: 100dvh;
            background: var(--maliev-bg);
            color: var(--maliev-ink);
            font-family: var(--maliev-font-sans);
            font-feature-settings: "liga" 1;
        }
        .login-shell {
            min-height: 100vh;
            min-height: 100dvh;
            display: grid;
            grid-template-rows: auto 1fr auto;
        }
        .login-header,
        .login-footer {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 16px;
            padding: 14px 20px;
            background: var(--maliev-panel);
        }
        .login-header {
            box-shadow: none;
        }
        .login-header-content {
            width: min(100%, 1080px);
            margin: 0 auto;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 16px;
        }
        .header-logo {
            display: flex;
            align-items: center;
            min-width: 0;
        }
        .header-logo-img {
            width: auto;
            max-height: 20px;
            display: block;
        }
        .header-logo-img--dark {
            display: none;
        }
        :root[data-maliev-theme="dark"] .header-logo-img--light {
            display: none;
        }
        :root[data-maliev-theme="dark"] .header-logo-img--dark {
            display: block;
        }
        .login-main {
            display: grid;
            place-items: center;
            min-height: 0;
            padding: 32px 16px;
        }
        .login-gateway-card {
            width: min(100%, 420px);
            padding: 30px;
            border: 0;
            border-radius: var(--maliev-radius-md);
            background: var(--maliev-panel);
            box-shadow: var(--maliev-shadow-card);
            color: var(--maliev-ink);
        }
        .login-card-heading {
            margin-bottom: 24px;
        }
        .gateway-kicker {
            margin: 0 0 8px;
            color: var(--maliev-muted);
            font-family: var(--maliev-font-mono);
            font-size: var(--maliev-type-caption);
            font-weight: 500;
            line-height: 1.3;
            letter-spacing: 0;
        }
        h1 {
            margin: 0;
            color: var(--maliev-ink);
            font-size: var(--maliev-type-h1);
            font-weight: 600;
            line-height: 1.2;
            letter-spacing: 0;
        }
        .login-title {
            display: flex;
            align-items: center;
            flex-wrap: wrap;
            gap: 0.34em;
        }
        .login-title-logo {
            display: inline-block;
            width: auto;
            height: 0.76em;
            transform: translateY(0.02em);
        }
        .login-title-logo--dark {
            display: none;
        }
        :root[data-maliev-theme="dark"] .login-title-logo--light {
            display: none;
        }
        :root[data-maliev-theme="dark"] .login-title-logo--dark {
            display: inline-block;
        }
        .subtitle {
            margin: 8px 0 0;
            color: var(--maliev-ink-2);
            font-size: var(--maliev-type-body);
            font-weight: 400;
            line-height: 1.5;
            letter-spacing: 0;
        }
        label {
            display: block;
            margin: 0 0 6px;
            color: var(--maliev-ink);
            font-size: var(--maliev-type-caption);
            font-weight: 500;
            line-height: 1.3;
            letter-spacing: 0;
        }
        .field {
            margin-bottom: 16px;
        }
        .password-label-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
            margin-bottom: 6px;
        }
        .password-hint {
            color: var(--maliev-muted);
            font-size: var(--maliev-type-caption);
            line-height: 1.3;
            white-space: nowrap;
        }
        input[type="email"],
        input[type="password"] {
            width: 100%;
            height: 42px;
            border: 0;
            border-radius: var(--maliev-radius-sm);
            padding: 0 12px;
            background: var(--maliev-panel);
            box-shadow: var(--maliev-shadow-ring);
            color: var(--maliev-ink);
            font: inherit;
            font-size: var(--maliev-type-body);
            letter-spacing: 0;
            transition: background 0.15s ease, box-shadow 0.15s ease;
        }
        input[type="email"]::placeholder,
        input[type="password"]::placeholder {
            color: var(--maliev-muted-2);
            opacity: 1;
        }
        input[type="email"]:focus,
        input[type="password"]:focus {
            outline: none;
            box-shadow: var(--maliev-focus-ring);
        }
        .submit,
        .btn-google,
        .theme-toggle-btn {
            border: 0;
            border-radius: var(--maliev-radius-sm);
            font-family: var(--maliev-font-sans);
            font-size: var(--maliev-type-body);
            font-weight: 600;
            letter-spacing: 0;
            cursor: pointer;
        }
        .submit,
        .btn-google {
            width: 100%;
            height: 42px;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            text-decoration: none;
            transition: background 0.15s ease, box-shadow 0.15s ease, transform 0.15s ease;
        }
        .submit {
            margin-top: 8px;
            background: var(--maliev-ink);
            color: var(--maliev-panel);
            box-shadow: var(--maliev-shadow-ring);
        }
        .submit:hover {
            transform: translateY(-1px);
            box-shadow: var(--maliev-shadow-md);
        }
        .btn-google {
            background: var(--maliev-panel);
            color: var(--maliev-ink);
            box-shadow: var(--maliev-shadow-ring);
        }
        .btn-google:hover {
            background: var(--maliev-panel-3);
            text-decoration: none;
        }
        .submit:focus-visible,
        .btn-google:focus-visible,
        .footer-link:focus-visible,
        .theme-toggle-btn:focus-visible {
            outline: none;
            box-shadow: var(--maliev-focus-ring);
        }
        .submit:active,
        .btn-google:active {
            transform: scale(0.99);
        }
        .google-logo {
            width: 18px;
            height: 18px;
            flex: 0 0 auto;
        }
        .divider {
            display: flex;
            align-items: center;
            gap: 12px;
            margin: 18px 0;
            color: var(--maliev-muted);
            font-size: var(--maliev-type-caption);
            font-weight: 400;
            line-height: 1.3;
            letter-spacing: 0;
        }
        .divider::before,
        .divider::after {
            content: "";
            flex: 1;
            height: 1px;
            background: var(--maliev-border);
        }
        .footer-note {
            margin: 24px 0 0;
            color: var(--maliev-ink-2);
            font-size: var(--maliev-type-caption);
            line-height: 1.45;
            text-align: center;
            letter-spacing: 0;
        }
        .footer-note a,
        .footer-link {
            color: var(--maliev-ink);
            font-weight: 600;
            text-decoration: none;
            border-radius: var(--maliev-radius-xs);
        }
        .footer-note a:hover,
        .footer-link:hover {
            text-decoration: underline;
        }
        .login-footer {
            color: var(--maliev-muted);
            font-size: var(--maliev-type-caption);
            line-height: 1.3;
            box-shadow: var(--maliev-shadow-ring);
        }
        .login-footer-content {
            width: min(100%, 1080px);
            margin: 0 auto;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
        }
        .theme-toggle-btn {
            width: 32px;
            height: 32px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            padding: 0;
            background: transparent;
            box-shadow: none;
            color: var(--maliev-ink-2);
        }
        .theme-toggle-btn:hover {
            background: var(--maliev-panel-3);
            color: var(--maliev-ink);
        }
        .theme-icon {
            display: none;
            width: 18px;
            height: 18px;
            stroke: currentColor;
        }
        :root[data-maliev-mode="light"] [data-theme-icon="light"],
        :root[data-maliev-mode="dark"] [data-theme-icon="dark"],
        :root[data-maliev-mode="system"] [data-theme-icon="system"] {
            display: block;
        }
        .error-alert {
            margin-bottom: 16px;
            border-radius: var(--maliev-radius-sm);
            padding: 10px 12px;
            color: var(--maliev-danger);
            background: var(--maliev-danger-bg);
            box-shadow: var(--maliev-shadow-ring);
            font-size: var(--maliev-type-body);
            line-height: 1.4;
        }
        @media (max-width: 640px) {
            .login-header,
            .login-footer {
                padding: 12px;
            }
            .login-main {
                align-items: start;
                padding: 18px 12px;
            }
            .login-gateway-card {
                padding: 22px;
            }
            .login-footer-content {
                flex-direction: column;
                text-align: center;
                gap: 6px;
            }
        }
    </style>
    <script>
        (function() {
            function cookie(name) {
                const value = `; ${document.cookie}`;
                const parts = value.split(`; ${name}=`);
                return parts.length === 2 ? parts.pop().split(';').shift() : null;
            }
            function localPreference() {
                try {
                    return localStorage.getItem('maliev_theme');
                } catch {
                    return null;
                }
            }
            function themePreference() {
                const pref = cookie('maliev_theme') || localPreference() || 'system';
                return pref === 'light' || pref === 'dark' || pref === 'system' ? pref : 'system';
            }
            function effectiveTheme(pref) {
                const systemDark = typeof matchMedia === 'function' && matchMedia('(prefers-color-scheme: dark)').matches;
                return pref === 'dark' || (pref === 'system' && systemDark) ? 'dark' : 'light';
            }
            function setButtonLabel(pref) {
                const button = document.querySelector('[data-theme-toggle]');
                if (!button) {
                    return;
                }

                const label = pref === 'dark' ? 'Dark theme' : pref === 'light' ? 'Light theme' : 'Auto theme';
                button.setAttribute('aria-label', label);
                button.setAttribute('title', label);
            }
            function applyTheme() {
                const pref = themePreference();
                document.documentElement.setAttribute('data-maliev-mode', pref);
                document.documentElement.setAttribute('data-maliev-theme', effectiveTheme(pref));
                setButtonLabel(pref);
            }
            window.toggleTheme = function() {
                const pref = themePreference();
                const next = pref === 'system' ? 'light' : pref === 'light' ? 'dark' : 'system';
                try {
                    localStorage.setItem('maliev_theme', next);
                } catch {
                    // Cookie persistence still keeps the server-owned login page in sync.
                }
                document.cookie = `maliev_theme=${next}; path=/; max-age=31536000; SameSite=Lax`;
                applyTheme();
            };
            if (typeof matchMedia === 'function') {
                matchMedia('(prefers-color-scheme: dark)').addEventListener('change', applyTheme);
            }
            applyTheme();
        })();
    </script>
</head>
<body>
    <div class="login-shell">
        <header class="login-header">
            <div class="login-header-content">
                <div class="header-logo">
                    <img src="/images/logo.svg" alt="MALIEV Logo" class="header-logo-img header-logo-img--light" />
                    <img src="/images/logo-white.svg" alt="MALIEV Logo" class="header-logo-img header-logo-img--dark" />
                </div>
                <button type="button" class="theme-toggle-btn" onclick="toggleTheme()" data-theme-toggle aria-label="Auto theme" title="Auto theme">
                    <svg class="theme-icon" data-theme-icon="light" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <path d="M12 4V2M12 22v-2M4 12H2M22 12h-2M5.64 5.64 4.22 4.22M19.78 19.78l-1.42-1.42M18.36 5.64l1.42-1.42M4.22 19.78l1.42-1.42" stroke-width="2" stroke-linecap="round"/>
                        <circle cx="12" cy="12" r="4" stroke-width="2"/>
                    </svg>
                    <svg class="theme-icon" data-theme-icon="dark" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <path d="M21 14.5A8.5 8.5 0 0 1 9.5 3a7 7 0 1 0 11.5 11.5Z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                    <svg class="theme-icon" data-theme-icon="system" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <rect x="3" y="4" width="18" height="12" rx="2" stroke-width="2"/>
                        <path d="M8 20h8M12 16v4M16.5 9.5h2v2M18.5 9.5 15 13" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                </button>
            </div>
        </header>
        <main class="login-main">
            <section class="login-gateway-card" aria-labelledby="login-title">
                <div class="login-card-heading">
                    <p class="gateway-kicker">Employee gateway</p>
                    <h1 id="login-title" class="login-title" aria-label="Sign in to MALIEV">
                        <span>Sign in to</span>
                        <img src="/images/logo.svg" alt="" aria-hidden="true" class="login-title-logo login-title-logo--light" />
                        <img src="/images/logo-white.svg" alt="" aria-hidden="true" class="login-title-logo login-title-logo--dark" />
                    </h1>
                    <p class="subtitle">Use your employee account to continue to the intranet workspace.</p>
                </div>
                {{errorHtml}}
                <form method="post" action="/api/v1/auth/login-form">
                    <input type="hidden" name="ReturnUrl" value="{{encodedReturnUrl}}" />
                    <div class="field">
                        <label for="Username">Email address</label>
                        <input id="Username" name="Username" type="email" autocomplete="username" placeholder="name@maliev.com" required />
                    </div>
                    <div class="field">
                        <div class="password-label-row">
                            <label for="Password">Password</label>
                            <span class="password-hint">Workspace password</span>
                        </div>
                        <input id="Password" name="Password" type="password" autocomplete="current-password" required />
                    </div>
                    <button class="submit" type="submit">Sign in</button>
                </form>
                <div class="divider">or</div>
                <a class="btn-google" href="/api/v1/auth/login?returnUrl={{encodedGoogleReturnUrl}}">
                    <svg class="google-logo" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                        <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
                        <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
                        <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05"/>
                        <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
                    </svg>
                    <span>Sign in with Google</span>
                </a>
                <p class="footer-note">
                    Not an employee? Visit our main page at
                    <a href="https://www.maliev.com" target="_blank">www.maliev.com</a>
                </p>
            </section>
        </main>
        <footer class="login-footer">
            <div class="login-footer-content">
                <span>MALIEV CO., LTD.</span>
                <a class="footer-link" href="https://www.maliev.com">www.maliev.com</a>
            </div>
        </footer>
    </div>
</body>
</html>
""";
    }

    private static string ToUserFacingLoginError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return string.Empty;
        }

        var normalized = WebUtility.HtmlDecode(error).Trim();
        if (string.Equals(normalized, InvalidCredentialsMessage, StringComparison.Ordinal))
        {
            return InvalidCredentialsMessage;
        }

        if (normalized.Contains("cancel", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("access_denied", StringComparison.OrdinalIgnoreCase))
        {
            return "Sign-in was cancelled. Please try again when you're ready.";
        }

        return TemporarySignInMessage;
    }
}

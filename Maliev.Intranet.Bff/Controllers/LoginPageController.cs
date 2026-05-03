using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Serves the BFF-owned employee login page without booting the WebAssembly app.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public sealed class LoginPageController : Controller
{
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
        var encodedError = WebUtility.HtmlEncode(error ?? string.Empty);
        var errorHtml = string.IsNullOrWhiteSpace(error)
            ? string.Empty
            : $"""<div class="error">{encodedError}</div>""";

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>MALIEV | Intranet Gateway</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Noto+Sans:wght@400;500;600;700&family=JetBrains+Mono:wght@500;600&display=swap" rel="stylesheet">
    <style>
        :root {
            color-scheme: light;
            --bg: #f5f7fa;
            --panel: #ffffff;
            --ink: #172033;
            --muted: #667085;
            --border: #e4e7ee;
            --accent-hue: 250;
            --accent: hsl(var(--accent-hue) 84% 56%);
        }
        :root[data-maliev-theme="dark"] {
            color-scheme: dark;
            --bg: #0d1117;
            --panel: #151b23;
            --ink: #e6edf3;
            --muted: #8b949e;
            --border: #30363d;
        }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            min-height: 100vh;
            font-family: "Noto Sans", system-ui, sans-serif;
            background: var(--bg);
            color: var(--ink);
        }
        .login-shell {
            min-height: 100vh;
            display: grid;
            grid-template-rows: auto 1fr auto;
        }
        .login-header,
        .login-footer {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 18px clamp(20px, 4vw, 48px);
            border-color: var(--border);
        }
        .login-header { border-bottom: 1px solid var(--border); }
        .login-footer { border-top: 1px solid var(--border); color: var(--muted); font-size: 12px; }
        .header-logo {
            display: flex;
            align-items: center;
            min-height: 32px;
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
            padding: 28px 18px;
        }
        .login-card {
            width: min(390px, 100%);
            padding: 26px;
            border: 1px solid var(--border);
            border-radius: 8px;
            background: var(--panel);
            box-shadow: 0 18px 48px rgba(14, 23, 43, 0.14);
        }
        h1 {
            margin: 0;
            font-size: 24px;
            letter-spacing: 0;
        }
        .subtitle {
            margin: 7px 0 24px;
            color: var(--muted);
            font-size: 13px;
        }
        label {
            display: block;
            margin: 14px 0 6px;
            font-size: 12px;
            font-weight: 700;
        }
        input[type="email"],
        input[type="password"] {
            width: 100%;
            height: 42px;
            border: 1px solid var(--border);
            border-radius: 6px;
            padding: 0 12px;
            background: transparent;
            color: var(--ink);
            font: inherit;
        }
        .submit,
        .btn-google,
        .theme-toggle-btn {
            height: 42px;
            border-radius: 6px;
            border: 1px solid var(--border);
            font-weight: 700;
            cursor: pointer;
        }
        .submit {
            width: 100%;
            margin-top: 18px;
            border-color: var(--accent);
            background: var(--accent);
            color: #fff;
        }
        .btn-google {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 12px;
            margin-top: 12px;
            color: var(--ink);
            text-decoration: none;
            background: var(--panel);
            box-shadow: 0 6px 16px rgba(14, 23, 43, 0.06);
        }
        .btn-google:hover {
            border-color: color-mix(in srgb, var(--accent), var(--border) 60%);
            text-decoration: none;
        }
        .google-logo {
            width: 20px;
            height: 20px;
            flex: 0 0 auto;
        }
        .divider {
            display: flex;
            align-items: center;
            gap: 10px;
            margin: 18px 0 6px;
            color: var(--muted);
            font-size: 11px;
            font-weight: 700;
        }
        .divider::before,
        .divider::after {
            content: "";
            flex: 1;
            height: 1px;
            background: var(--border);
        }
        .theme-toggle-btn {
            width: 42px;
            background: transparent;
            color: var(--ink);
            display: inline-flex;
            align-items: center;
            justify-content: center;
            padding: 0;
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
        .error {
            margin-bottom: 14px;
            border: 1px solid #ef4444;
            border-radius: 6px;
            padding: 10px 12px;
            color: #b91c1c;
            background: rgba(239, 68, 68, 0.08);
            font-size: 13px;
        }
        @media (max-width: 520px) {
            .login-header,
            .login-footer { padding: 14px 18px; }
            .login-footer { display: block; }
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

                const label = pref === 'dark' ? 'Dark Theme' : pref === 'light' ? 'Light Theme' : 'Auto Theme';
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
            <div class="header-logo">
                <img src="/images/logo.svg" alt="MALIEV Logo" class="header-logo-img header-logo-img--light" />
                <img src="/images/logo-white.svg" alt="MALIEV Logo" class="header-logo-img header-logo-img--dark" />
            </div>
            <button type="button" class="theme-toggle-btn" onclick="toggleTheme()" data-theme-toggle aria-label="Auto Theme" title="Auto Theme">
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
        </header>
        <main class="login-main">
            <section class="login-card">
                <h1>Intranet</h1>
                <p class="subtitle">Enter your employee credentials to continue.</p>
                {{errorHtml}}
                <form method="post" action="/api/v1/auth/login-form">
                    <input type="hidden" name="ReturnUrl" value="{{encodedReturnUrl}}" />
                    <label for="Username">Email address</label>
                    <input id="Username" name="Username" type="email" autocomplete="username" placeholder="name@maliev.com" required />
                    <label for="Password">Password</label>
                    <input id="Password" name="Password" type="password" autocomplete="current-password" required />
                    <button class="submit" type="submit">SIGN IN</button>
                </form>
                <div class="divider">OR</div>
                <a class="btn-google" href="/api/v1/auth/login?returnUrl={{encodedGoogleReturnUrl}}">
                    <svg class="google-logo" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                        <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
                        <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
                        <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05"/>
                        <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
                    </svg>
                    <span>Sign in with Google</span>
                </a>
            </section>
        </main>
        <footer class="login-footer">
            <span>MALIEV Co., Ltd.</span>
            <span>www.maliev.com</span>
        </footer>
    </div>
</body>
</html>
""";
    }
}

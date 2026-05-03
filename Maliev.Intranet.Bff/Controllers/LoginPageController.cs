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
        .brand {
            display: flex;
            align-items: center;
            gap: 10px;
            font-weight: 700;
            letter-spacing: 0;
        }
        .brand-mark {
            width: 32px;
            height: 32px;
            border-radius: 7px;
            display: grid;
            place-items: center;
            background: var(--accent);
            color: #fff;
            font-family: "JetBrains Mono", monospace;
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
        .google,
        .theme {
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
        .google {
            display: flex;
            align-items: center;
            justify-content: center;
            margin-top: 12px;
            color: var(--ink);
            text-decoration: none;
            background: transparent;
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
        .theme {
            width: 42px;
            background: transparent;
            color: var(--ink);
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
            function applyTheme() {
                let pref = cookie('maliev_theme') || localStorage.getItem('maliev_theme') || 'system';
                const dark = pref === 'dark' || (pref === 'system' && matchMedia('(prefers-color-scheme: dark)').matches);
                document.documentElement.setAttribute('data-maliev-theme', dark ? 'dark' : 'light');
                const accent = localStorage.getItem('maliev_accent_hue') || cookie('maliev_accent_hue') || '250';
                document.documentElement.style.setProperty('--accent-hue', accent);
            }
            window.toggleTheme = function() {
                const current = document.documentElement.getAttribute('data-maliev-theme') === 'dark' ? 'light' : 'dark';
                localStorage.setItem('maliev_theme', current);
                document.cookie = `maliev_theme=${current}; path=/; max-age=31536000; SameSite=Lax`;
                applyTheme();
            };
            applyTheme();
        })();
    </script>
</head>
<body>
    <div class="login-shell">
        <header class="login-header">
            <div class="brand"><span class="brand-mark">M</span><span>MALIEV Intranet</span></div>
            <button type="button" class="theme" onclick="toggleTheme()" aria-label="Toggle theme">A</button>
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
                <a class="google" href="/api/v1/auth/login?returnUrl={{encodedGoogleReturnUrl}}">Sign in with Google</a>
            </section>
        </main>
        <footer class="login-footer">
            <span>MALIEV INC. Employee systems</span>
            <span>www.maliev.com</span>
        </footer>
    </div>
</body>
</html>
""";
    }
}

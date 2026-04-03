using System.Net;
using Microsoft.AspNetCore.SignalR.Client;

namespace Maliev.Intranet.Client.Helpers;

/// <summary>
/// Extension methods for <see cref="HubConnectionBuilder"/> that configure
/// cookie-forwarding credentials when running on the server (InteractiveServer mode).
/// In WASM the browser automatically sends cookies, so no action is needed.
/// </summary>
public static class HubConnectionBuilderExtensions
{
    /// <summary>
    /// Configures the hub connection URL and forwards the caller's cookies
    /// when running inside a Blazor server-side circuit.
    /// When running in the browser (WASM) cookies are sent automatically.
    /// </summary>
    /// <param name="builder">The <see cref="IHubConnectionBuilder"/> to configure.</param>
    /// <param name="url">The URL of the SignalR hub.</param>
    /// <param name="cookieHeader">
    /// Optional raw HTTP "Cookie" header captured during SSR.
    /// Ignored when running in the browser.
    /// </param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static IHubConnectionBuilder WithUrlAndCookies(
        this IHubConnectionBuilder builder,
        string url,
        string? cookieHeader = null)
    {
        builder.WithUrl(url, options =>
        {
            if (!OperatingSystem.IsBrowser() && !string.IsNullOrEmpty(cookieHeader))
            {
                var uri = new Uri(url);
                var container = new CookieContainer();

                foreach (var pair in cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var eq = pair.IndexOf('=');
                    if (eq <= 0) continue;

                    var name = pair[..eq].Trim();
                    var value = pair[(eq + 1)..].Trim();
                    container.Add(uri, new Cookie(name, value));
                }

                options.Cookies = container;
            }
        });

        return builder;
    }
}

using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Circuit-scoped service that captures the browser cookie header during
/// SSR / prerendering so that server-side SignalR hub connections can
/// forward the user's authentication cookies.
/// <para>
/// During SSR the cookie is set directly via <see cref="CookieHeader"/>.
/// In the Blazor circuit the value is lazily loaded from
/// <see cref="PersistentComponentState"/> (which was populated during
/// prerendering) so the cookie survives the scope transition.
/// </para>
/// </summary>
public class CookieProvider
{
    /// <summary>
    /// The key used to persist the cookie header in
    /// <see cref="PersistentComponentState"/> during prerendering.
    /// </summary>
    public const string StateKey = "__Maliev_SsrCookies__";

    private readonly PersistentComponentState? _persistentState;
    private string? _cookieHeader;
    private bool _loaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="CookieProvider"/> class.
    /// </summary>
    /// <param name="persistentState">
    /// Optional <see cref="PersistentComponentState"/> for retrieving cookies
    /// from SSR prerendering. When <c>null</c> (e.g. in unit tests), cookies
    /// must be set manually via <see cref="CookieHeader"/>.
    /// </param>
    public CookieProvider(PersistentComponentState? persistentState = null)
    {
        _persistentState = persistentState;
    }

    /// <summary>
    /// Gets or sets the raw HTTP "Cookie" header captured from the initial SSR request.
    /// <para>
    /// When read for the first time in an interactive Blazor circuit the value is
    /// lazily loaded from <see cref="PersistentComponentState"/> (if available).
    /// </para>
    /// </summary>
    public string? CookieHeader
    {
        get
        {
            if (!_loaded)
            {
                if (_persistentState is not null)
                {
                    _persistentState.TryTakeFromJson(StateKey, out _cookieHeader);
                }

                _loaded = true;
            }

            return _cookieHeader;
        }
        set
        {
            _cookieHeader = value;
            _loaded = true;
        }
    }
}

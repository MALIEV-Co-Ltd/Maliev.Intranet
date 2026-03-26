using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;

namespace Maliev.Intranet.Bff;

/// <summary>
/// Server-side authentication state provider that persists state for interactive components.
/// </summary>
public class PersistingRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly PersistentComponentState _state;
    private readonly PersistingComponentStateSubscription _subscription;

    private Task<AuthenticationState>? _authenticationStateTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistingRevalidatingAuthenticationStateProvider"/> class.
    /// </summary>
    public PersistingRevalidatingAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        PersistentComponentState persistentComponentState)
        : base(loggerFactory)
    {
        _state = persistentComponentState;
        AuthenticationStateChanged += OnAuthenticationStateChanged;
        _subscription = _state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
    }

    /// <inheritdoc />
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    /// <inheritdoc />
    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        var user = authenticationState.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(true);
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> authenticationStateTask)
    {
        _authenticationStateTask = authenticationStateTask;
    }

    private async Task OnPersistingAsync()
    {
        if (_authenticationStateTask is null)
        {
            throw new InvalidOperationException("Authentication state not set in " +
                nameof(OnPersistingAsync) + "().");
        }

        var authenticationState = await _authenticationStateTask;
        var principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated == true && principal.Identity is ClaimsIdentity claimsIdentity)
        {
            var userInfo = new UserInfo
            {
                NameClaimType = claimsIdentity.NameClaimType,
                RoleClaimType = claimsIdentity.RoleClaimType,
                Claims = principal.Claims.Select(c => new ClaimInfo { Type = c.Type, Value = c.Value }).ToList()
            };

            _state.PersistAsJson(nameof(UserInfo), userInfo);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _subscription.Dispose();
        AuthenticationStateChanged -= OnAuthenticationStateChanged;
        base.Dispose(disposing);
    }

    private sealed class UserInfo
    {
        public string NameClaimType { get; set; } = string.Empty;
        public string RoleClaimType { get; set; } = string.Empty;
        public List<ClaimInfo> Claims { get; set; } = [];
    }

    private sealed class ClaimInfo
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}

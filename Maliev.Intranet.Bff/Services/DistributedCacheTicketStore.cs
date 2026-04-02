using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Stores authentication tickets in a server-side concurrent dictionary to avoid browser cookie size limits.
/// The cookie itself only holds a unique session ID (~36 bytes) instead of the full encrypted ticket (~7KB).
/// </summary>
public sealed class DistributedCacheTicketStore : ITicketStore
{
    private readonly ConcurrentDictionary<string, AuthenticationTicket> _tickets = new();
    private static readonly TimeSpan TicketExpiration = TimeSpan.FromHours(8);

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedCacheTicketStore"/>.
    /// </summary>
    public DistributedCacheTicketStore()
    {
    }

    /// <inheritdoc />
    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        _tickets.TryGetValue(key, out var ticket);
        return Task.FromResult(ticket);
    }

    /// <inheritdoc />
    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var userId = ticket.Principal.FindFirst("sub")?.Value
            ?? ticket.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Cannot store ticket: sub claim is missing. Ensure the user has a valid sub claim before authentication.");

        var expiresUtc = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.Add(TicketExpiration);
        ticket.Properties.ExpiresUtc = expiresUtc;

        _tickets[userId] = ticket;
        return Task.FromResult(userId);
    }

    /// <inheritdoc />
    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var expiresUtc = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.Add(TicketExpiration);
        ticket.Properties.ExpiresUtc = expiresUtc;
        _tickets[key] = ticket;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        _tickets.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}

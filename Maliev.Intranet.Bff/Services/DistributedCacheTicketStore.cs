using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Stores authentication tickets in a server-side concurrent dictionary to avoid browser cookie size limits.
/// The cookie itself only holds a short session key (~50 bytes) instead of the full encrypted ticket (~7KB).
/// </summary>
public sealed class DistributedCacheTicketStore : ITicketStore
{
    private readonly ConcurrentDictionary<string, AuthenticationTicket> _tickets = new();
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan TicketExpiration = TimeSpan.FromHours(8);

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedCacheTicketStore"/>.
    /// </summary>
    /// <param name="cache">The memory cache instance.</param>
    public DistributedCacheTicketStore(IMemoryCache cache)
    {
        _cache = cache;
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
        var key = GenerateSecureKey();
        var expiresUtc = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.Add(TicketExpiration);
        ticket.Properties.ExpiresUtc = expiresUtc;
        _tickets[key] = ticket;
        return Task.FromResult(key);
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

    private static string GenerateSecureKey()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}

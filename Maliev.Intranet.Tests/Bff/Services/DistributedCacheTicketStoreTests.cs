using System.Security.Claims;
using Maliev.Intranet.Bff.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Xunit;

namespace Maliev.Intranet.Tests.Bff.Services;

public class DistributedCacheTicketStoreTests
{
    private static AuthenticationTicket CreateTicket(string? sub = null, string? nameIdentifier = null)
    {
        var claims = new List<Claim>();
        if (sub != null) claims.Add(new Claim("sub", sub));
        if (nameIdentifier != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties();
        return new AuthenticationTicket(principal, properties, "Test");
    }

    [Fact]
    public async Task StoreAsync_ReturnsSubAsKey()
    {
        var store = new DistributedCacheTicketStore();
        var ticket = CreateTicket(sub: "user-123");

        var key = await store.StoreAsync(ticket);

        Assert.Equal("user-123", key);
    }

    [Fact]
    public async Task StoreAsync_ReturnsNameIdentifier_WhenSubMissing()
    {
        var store = new DistributedCacheTicketStore();
        var ticket = CreateTicket(nameIdentifier: "guid-user-456");

        var key = await store.StoreAsync(ticket);

        Assert.Equal("guid-user-456", key);
    }

    [Fact]
    public async Task StoreAsync_Throws_WhenNoIdentifierClaim()
    {
        var store = new DistributedCacheTicketStore();
        var ticket = CreateTicket();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => store.StoreAsync(ticket));
        Assert.Contains("sub claim is missing", ex.Message);
    }

    [Fact]
    public async Task StoreAsync_SetsExpiresUtc_WhenNotSet()
    {
        var store = new DistributedCacheTicketStore();
        var ticket = CreateTicket(sub: "user-123");
        Assert.Null(ticket.Properties.ExpiresUtc);

        await store.StoreAsync(ticket);

        Assert.NotNull(ticket.Properties.ExpiresUtc);
        Assert.True(ticket.Properties.ExpiresUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task StoreAsync_Overwrites_WhenSameUserStoresTwice()
    {
        var store = new DistributedCacheTicketStore();
        var ticket1 = CreateTicket(sub: "user-123");
        var ticket2 = CreateTicket(sub: "user-123");

        var key1 = await store.StoreAsync(ticket1);
        var key2 = await store.StoreAsync(ticket2);

        Assert.Equal(key1, key2);
        var retrieved = await store.RetrieveAsync(key1);
        Assert.NotNull(retrieved);
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsStoredTicket()
    {
        var store = new DistributedCacheTicketStore();
        var ticket = CreateTicket(sub: "user-123");

        var key = await store.StoreAsync(ticket);
        var retrieved = await store.RetrieveAsync(key);

        Assert.NotNull(retrieved);
        Assert.Equal("user-123", retrieved.Principal.FindFirst("sub")?.Value);
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsNull_WhenKeyNotFound()
    {
        var store = new DistributedCacheTicketStore();

        var result = await store.RetrieveAsync("nonexistent-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task RenewAsync_UpdatesExpiration()
    {
        var store = new DistributedCacheTicketStore();
        var ticket = CreateTicket(sub: "user-123");
        var key = await store.StoreAsync(ticket);

        var originalExpiry = ticket.Properties.ExpiresUtc;
        await Task.Delay(10);

        var newTicket = CreateTicket(sub: "user-123");
        newTicket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(10);
        await store.RenewAsync(key, newTicket);

        var retrieved = await store.RetrieveAsync(key);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.Properties.ExpiresUtc > originalExpiry);
    }

    [Fact]
    public async Task RemoveAsync_DeletesTicket()
    {
        var store = new DistributedCacheTicketStore();
        var ticket = CreateTicket(sub: "user-123");
        var key = await store.StoreAsync(ticket);

        await store.RemoveAsync(key);

        var result = await store.RetrieveAsync(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task MultipleUsers_EachHasOwnEntry()
    {
        var store = new DistributedCacheTicketStore();
        var ticketA = CreateTicket(sub: "user-a");
        var ticketB = CreateTicket(sub: "user-b");

        var keyA = await store.StoreAsync(ticketA);
        var keyB = await store.StoreAsync(ticketB);

        Assert.NotEqual(keyA, keyB);

        var retrievedA = await store.RetrieveAsync(keyA);
        var retrievedB = await store.RetrieveAsync(keyB);

        Assert.NotNull(retrievedA);
        Assert.NotNull(retrievedB);
        Assert.Equal("user-a", retrievedA.Principal.FindFirst("sub")?.Value);
        Assert.Equal("user-b", retrievedB.Principal.FindFirst("sub")?.Value);
    }
}

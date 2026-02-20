using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class AccountingServiceClientTests
{
    [Fact]
    public async Task GetAccountsTreeAsync_ShouldReturnData()
    {
        var response = new List<ChartOfAccountDto>();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(httpResponse));
        var client = new AccountingServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var result = await client.GetAccountsTreeAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetJournalEntriesAsync_ShouldReturnData()
    {
        var response = new PagedResponse<JournalEntryDto>();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(httpResponse));
        var client = new AccountingServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var result = await client.GetJournalEntriesAsync();

        Assert.NotNull(result);
    }
}

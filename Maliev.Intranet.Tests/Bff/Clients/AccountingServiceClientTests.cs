using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class AccountingServiceClientTests
{
    [Fact]
    public async Task GetAccountsTreeAsync_TargetsChartOfAccountsHierarchyRoute()
    {
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    accountNumber = "1000",
                    name = "Cash",
                    type = "Asset",
                    isActive = true,
                    children = Array.Empty<object>()
                }
            });
        });

        var result = await client.GetAccountsTreeAsync("Asset");

        Assert.NotNull(capturedRequest);
        Assert.Equal("/accounting/v1/chart-of-accounts/hierarchy?accountType=Asset", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        Assert.Equal("1000", result.Single().Code);
    }

    [Fact]
    public async Task GetJournalEntriesAsync_TargetsJournalEntriesRouteWithPagination()
    {
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    entryNumber = "JE-1",
                    entryDate = DateTime.UtcNow.Date,
                    description = "Test",
                    status = "Draft",
                    totalDebit = 100m,
                    totalCredit = 100m,
                    createdAt = DateTime.UtcNow,
                    lines = Array.Empty<object>()
                }
            });
        });

        var result = await client.GetJournalEntriesAsync(page: 2, pageSize: 10, status: "Draft");

        Assert.NotNull(capturedRequest);
        Assert.Equal("/accounting/v1/journal-entries?pageNumber=2&pageSize=10&status=Draft", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(2, result.Meta.CurrentPage);
    }

    [Fact]
    public async Task CreateJournalEntryAsync_UsesRealJournalEntryPayloadNames()
    {
        string? payload = null;
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(async request =>
        {
            capturedRequest = request;
            payload = await request.Content!.ReadAsStringAsync();
            return JsonContent.Create(new
            {
                id = Guid.NewGuid(),
                entryNumber = "JE-2",
                entryDate = DateTime.UtcNow.Date,
                description = "Income",
                status = "Draft",
                totalDebit = 250m,
                totalCredit = 250m,
                createdAt = DateTime.UtcNow,
                lines = Array.Empty<object>()
            });
        });

        var accountId = Guid.NewGuid();
        await client.CreateJournalEntryAsync(new CreateJournalEntryRequest
        {
            Date = new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
            Description = "Income",
            Reference = "receipt-42",
            Lines = [new JournalEntryLineDto { AccountId = accountId, Debit = 250m, Reference = "receipt-42" }]
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/accounting/v1/journal-entries", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(payload);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("entryDate", out _));
        Assert.Equal("receipt-42", root.GetProperty("reference").GetString());
        Assert.Equal(accountId, root.GetProperty("lines")[0].GetProperty("accountId").GetGuid());
        Assert.Equal(250m, root.GetProperty("lines")[0].GetProperty("debitAmount").GetDecimal());
        Assert.Equal("receipt-42", root.GetProperty("lines")[0].GetProperty("reference").GetString());
    }

    [Theory]
    [InlineData("income-statement", "/accounting/v1/reports/income-statement?startDate=2026-05-01&endDate=2026-05-31")]
    [InlineData("balance-sheet", "/accounting/v1/reports/balance-sheet?asOfDate=2026-05-31")]
    [InlineData("trial-balance", "/accounting/v1/reports/trial-balance?startDate=2026-05-01&endDate=2026-05-31")]
    public async Task GetReportAsync_TargetsActualReportRoutes(string type, string expectedPath)
    {
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new { generatedAt = DateTime.UtcNow, revenues = Array.Empty<object>(), expenses = Array.Empty<object>(), items = Array.Empty<object>() });
        });

        await client.GetReportAsync(type, new DateTime(2026, 05, 01), new DateTime(2026, 05, 31));

        Assert.NotNull(capturedRequest);
        Assert.Equal(expectedPath, capturedRequest.RequestUri!.PathAndQuery);
    }

    private static AccountingServiceClient MakeClient(Func<HttpRequestMessage, HttpContent> contentFactory)
    {
        var handler = new MockHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = contentFactory(request)
            }));

        return new AccountingServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        });
    }

    private static AccountingServiceClient MakeClient(Func<HttpRequestMessage, Task<HttpContent>> contentFactory)
    {
        var handler = new MockHttpMessageHandler(async (request, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = await contentFactory(request)
            });

        return new AccountingServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        });
    }
}

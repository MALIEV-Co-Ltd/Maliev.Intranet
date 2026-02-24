using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

/// <summary>Tests for the career service client.</summary>
public class CareerServiceClientTests
{
    private readonly MockHttpMessageHandler _handler;
    private readonly CareerServiceClient _client;

    /// <summary>Initializes a new instance of the <see cref="CareerServiceClientTests"/> class.</summary>
    public CareerServiceClientTests()
    {
        _handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://test") };
        _client = new CareerServiceClient(httpClient);
    }

    /// <summary>Verifies that getting job postings returns data.</summary>
    [Fact]
    public async Task GetJobPostingsAsync_ShouldReturnData()
    {
        var response = new MalievResponse<List<JobPostingSummaryDto>> { Data = new List<JobPostingSummaryDto>() };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };

        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(httpResponse));
        var client = new CareerServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var result = await client.GetJobPostingsAsync();

        Assert.NotNull(result);
    }

    /// <summary>Verifies that getting recruitment stats returns data.</summary>
    [Fact]
    public async Task GetRecruitmentStatsAsync_ShouldReturnData()
    {
        var response = new RecruitmentStatsDto();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };

        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(httpResponse));
        var client = new CareerServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var result = await client.GetRecruitmentStatsAsync();

        Assert.NotNull(result);
    }
}

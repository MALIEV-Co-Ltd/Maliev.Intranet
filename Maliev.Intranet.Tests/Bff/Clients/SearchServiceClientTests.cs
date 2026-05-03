using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class SearchServiceClientTests
{
    [Fact]
    public async Task SearchAsync_UsesSearchServiceWireShape()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SearchServiceResponseDto(
                    "acme bolt",
                    0,
                    []))
            });
        });

        var client = new SearchServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        });

        await client.SearchAsync("acme bolt", 5);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal("/search/v1/search?query=acme%20bolt&limit=5", capturedRequest.RequestUri!.PathAndQuery);
    }
}

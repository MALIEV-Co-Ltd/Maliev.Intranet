using System.Net;

namespace Maliev.Intranet.Tests.Testing;

public class MockHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? HandlerFunc { get; set; }

    public MockHttpMessageHandler()
    {
    }

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        HandlerFunc = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (HandlerFunc != null)
        {
            return await HandlerFunc(request, cancellationToken);
        }
        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}

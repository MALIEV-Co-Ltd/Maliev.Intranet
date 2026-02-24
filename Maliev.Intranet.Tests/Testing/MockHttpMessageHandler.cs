using System.Net;

namespace Maliev.Intranet.Tests.Testing;

/// <summary>
/// Fluent mock HTTP handler for unit tests.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly List<MockedRequest> _requests = [];

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? HandlerFunc { get; set; }

    public MockHttpMessageHandler()
    {
    }

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        HandlerFunc = handler;
    }

    /// <summary>Registers an expected request URL pattern and returns a builder to configure the response.</summary>
    public MockedRequest When(string urlPattern)
    {
        var mocked = new MockedRequest(urlPattern);
        _requests.Add(mocked);
        return mocked;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var match = _requests.FirstOrDefault(r => r.Matches(request));
        if (match != null)
            return match.GetResponse(request);

        if (HandlerFunc != null)
            return await HandlerFunc(request, cancellationToken);

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}

/// <summary>
/// Represents a registered mock request with a configured response.
/// </summary>
public class MockedRequest
{
    private readonly string _urlPattern;
    private Func<HttpRequestMessage, HttpResponseMessage>? _responseFactory;

    internal MockedRequest(string urlPattern)
    {
        _urlPattern = urlPattern;
    }

    /// <summary>Configures the response for this request pattern.</summary>
    public MockedRequest Respond(HttpStatusCode statusCode, HttpContent content)
    {
        _responseFactory = _ => new HttpResponseMessage(statusCode) { Content = content };
        return this;
    }

    internal bool Matches(HttpRequestMessage request)
    {
        var uri = request.RequestUri?.ToString() ?? string.Empty;
        return uri.Contains(_urlPattern);
    }

    internal HttpResponseMessage GetResponse(HttpRequestMessage request)
    {
        return _responseFactory?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK);
    }
}

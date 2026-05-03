using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public sealed class NotificationServiceClientTests
{
    [Fact]
    public async Task GetTemplatesAsync_MapsNotificationServicePaginatedItemsShape()
    {
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new
            {
                items = new[]
                {
                    new
                    {
                        id = Guid.NewGuid(),
                        templateKey = "order-confirmed",
                        version = 1,
                        language = "en",
                        channelType = 0,
                        contentTemplate = "Hello {{name}}"
                    }
                },
                page = 2,
                pageSize = 5,
                totalCount = 11,
                totalPages = 3
            });
        });

        var result = await client.GetTemplatesAsync(page: 2, pageSize: 5, filter: "order");

        Assert.NotNull(capturedRequest);
        Assert.Equal("/notification/v1/templates?page=2&pageSize=5&filter=order", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        var template = Assert.Single(result.Data);
        Assert.Equal("order-confirmed", template.TemplateKey);
        Assert.Equal("Hello {{name}}", template.BodyTemplate);
        Assert.Equal("email", template.ChannelType);
        Assert.Equal(2, result.Meta.CurrentPage);
        Assert.Equal(11, result.Meta.TotalCount);
    }

    [Fact]
    public async Task CreateTemplateAsync_PostsNotificationServiceContractShape()
    {
        string? payload = null;
        var client = MakeClient(async request =>
        {
            payload = await request.Content!.ReadAsStringAsync();
            return JsonContent.Create(new
            {
                id = Guid.NewGuid(),
                templateKey = "payment-failed",
                version = 1,
                language = "en",
                channelType = 0,
                contentTemplate = "Payment {{amount}} failed"
            });
        });

        await client.CreateTemplateAsync(new()
        {
            TemplateKey = "payment-failed",
            Name = "Payment failed",
            ChannelType = "email",
            Language = "en",
            SubjectTemplate = "Payment failed",
            BodyTemplate = "Payment {{amount}} failed"
        });

        Assert.NotNull(payload);
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.Equal("payment-failed", root.GetProperty("templateKey").GetString());
        Assert.Equal(0, root.GetProperty("channelType").GetInt32());
        Assert.Equal("Payment {{amount}} failed", root.GetProperty("contentTemplate").GetString());
        Assert.Equal("amount", root.GetProperty("parameters")[0].GetString());
    }

    private static NotificationServiceClient MakeClient(Func<HttpRequestMessage, HttpContent> contentFactory)
    {
        var handler = new MockHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = contentFactory(request)
            }));

        return new NotificationServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        });
    }

    private static NotificationServiceClient MakeClient(Func<HttpRequestMessage, Task<HttpContent>> contentFactory)
    {
        var handler = new MockHttpMessageHandler(async (request, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = await contentFactory(request)
            });

        return new NotificationServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        });
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Tests.Testing;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Shared;

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

    [Fact]
    public async Task DispatchEventAsync_PostsNotificationEventContractShape()
    {
        HttpRequestMessage? capturedRequest = null;
        string? payload = null;
        var eventId = Guid.NewGuid();
        var client = MakeClient(async request =>
        {
            capturedRequest = request;
            payload = await request.Content!.ReadAsStringAsync();
            return JsonContent.Create(new { messageId = eventId });
        });

        await client.DispatchEventAsync(new NotificationEvent(
            MessageId: eventId,
            MessageName: nameof(NotificationEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "Maliev.IntranetBff",
            ConsumedBy: ["Maliev.NotificationService"],
            CorrelationId: eventId,
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new NotificationEventPayload(
                NotificationType: "Quote update",
                Priority: "standard",
                TargetUsers: [new NotificationEventPayloadTargetUsersItem("customer-principal", "customer")],
                TemplateId: string.Empty,
                Parameters: new Dictionary<string, string>
                {
                    ["subject"] = "Quote update",
                    ["message"] = "Your quote is ready."
                },
                Metadata: new NotificationEventPayloadMetadata("en", "intranet-customer-detail"))));

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/notification/v1/events", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(payload);
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.Equal(eventId, root.GetProperty("messageId").GetGuid());
        Assert.Equal("Quote update", root.GetProperty("payload").GetProperty("notificationType").GetString());
        Assert.Equal("standard", root.GetProperty("payload").GetProperty("priority").GetString());
        Assert.Equal("customer-principal", root.GetProperty("payload").GetProperty("targetUsers")[0].GetProperty("userId").GetString());
    }

    [Fact]
    public async Task GetDeliveryLogsAsync_MapsProviderMessageId()
    {
        var eventId = Guid.NewGuid().ToString();
        var providerMessageId = $"email_simulated_{Guid.NewGuid():N}";
        var client = MakeClient(_ => JsonContent.Create(new
        {
            items = new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    eventId,
                    userId = "customer-principal",
                    channelType = "email",
                    recipientIdentifier = "customer-principal",
                    messageContent = "Quote update",
                    providerResponse = "Email sent successfully",
                    providerMessageId,
                    attemptNumber = 1,
                    createdAt = DateTimeOffset.UtcNow,
                    status = "delivered",
                    deliveredAt = DateTimeOffset.UtcNow
                }
            },
            page = 1,
            pageSize = 20,
            totalCount = 1,
            totalPages = 1
        }));

        var result = await client.GetDeliveryLogsAsync();

        Assert.NotNull(result);
        var log = Assert.Single(result.Data);
        Assert.Equal(eventId, log.EventId);
        Assert.Equal("email", log.ChannelType);
        Assert.Equal("delivered", log.Status);
        Assert.Equal("Email sent successfully", log.Error);
        Assert.Equal(providerMessageId, log.ProviderMessageId);
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

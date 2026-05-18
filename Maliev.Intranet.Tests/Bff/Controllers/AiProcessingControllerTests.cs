using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class AiProcessingControllerTests
{
    private readonly Mock<ChatbotServiceClient> _chatbotClientMock;
    private readonly AiProcessingController _controller;

    public AiProcessingControllerTests()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler())
        {
            BaseAddress = new Uri("http://test")
        };

        _chatbotClientMock = new Mock<ChatbotServiceClient>(
            httpClient,
            new Mock<ILogger<ChatbotServiceClient>>().Object);

        _controller = new AiProcessingController(
            _chatbotClientMock.Object,
            new UploadServiceClient(httpClient),
            new RegistryServiceClient(httpClient),
            new CustomerServiceClient(httpClient, new Mock<ILogger<CustomerServiceClient>>().Object),
            new Mock<ILogger<AiProcessingController>>().Object);
    }

    [Fact]
    public async Task Health_WhenChatbotHealthy_DoesNotCreateChatSession()
    {
        _chatbotClientMock
            .Setup(client => client.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Health(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _chatbotClientMock.Verify(
            client => client.InitiateSessionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Health_WhenChatbotUnavailable_ReturnsServiceUnavailable()
    {
        _chatbotClientMock
            .Setup(client => client.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Health(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetDownloadUrl_WhenReferenceIsUploadId_UsesUploadIdSignedUrlEndpoint()
    {
        HttpRequestMessage? downstreamRequest = null;
        var controller = CreateController((request, _) =>
        {
            downstreamRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl = "https://signed.example/customer.pdf" })
            });
        });

        var result = await controller.GetDownloadUrl("upload-123", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("https://signed.example/customer.pdf", ReadUrl(ok.Value));
        Assert.NotNull(downstreamRequest);
        Assert.Equal("/upload/v1/files/upload-123/signed-url", downstreamRequest.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetDownloadUrl_WhenReferenceIsStoragePath_UsesByPathSignedUrlEndpoint()
    {
        HttpRequestMessage? downstreamRequest = null;
        var controller = CreateController((request, _) =>
        {
            downstreamRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl = "https://signed.example/customer.pdf" })
            });
        });

        var result = await controller.GetDownloadUrl("customers/customer-1/customer.pdf", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("https://signed.example/customer.pdf", ReadUrl(ok.Value));
        Assert.NotNull(downstreamRequest);
        Assert.Equal("/upload/v1/files/by-path/signed-url", downstreamRequest.RequestUri?.AbsolutePath);
        var payload = JsonDocument.Parse(await downstreamRequest.Content!.ReadAsStringAsync()).RootElement;
        Assert.Equal("customers/customer-1/customer.pdf", payload.GetProperty("storagePath").GetString());
    }

    [Fact]
    public async Task ExtractSupplierFromDocument_WhenAiReturnsJson_ReturnsSupplierExtraction()
    {
        _chatbotClientMock
            .Setup(client => client.InitiateSessionAsync("intranet", "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatbotSessionResponse
            {
                SessionId = Guid.Parse("f660edb5-a9b8-4e46-a92c-b4d54b266466"),
                Language = "en"
            });

        _chatbotClientMock
            .Setup(client => client.SendMessageAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<List<ChatbotAttachment>?>(),
                "application/json",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatbotMessageResponse
            {
                Content = """
                    {
                      "supplier_name": "Thai Metals Supply",
                      "email": "sales@thai-metals.example",
                      "phone": "+66 2 555 0101",
                      "country": "Thailand",
                      "capabilities": ["CNC", "Anodizing"]
                    }
                    """
            });

        var result = await _controller.ExtractSupplierFromDocument(new FormFileCollection(), "Thai Metals Supply sales@thai-metals.example");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var extracted = Assert.IsType<ExtractedSupplierDataResponse>(ok.Value);
        Assert.Equal("Thai Metals Supply", extracted.SupplierName);
        Assert.Equal("sales@thai-metals.example", extracted.Email);
        Assert.Equal("CNC", extracted.Capabilities[0]);
        Assert.True(extracted.Confidence > 0);
    }

    [Fact]
    public async Task ExtractAccountingEntryFromDocument_WhenAiReturnsJson_ReturnsDraftJournalFields()
    {
        _chatbotClientMock
            .Setup(client => client.InitiateSessionAsync("intranet", "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatbotSessionResponse
            {
                SessionId = Guid.Parse("31f9957d-326f-4a4d-b747-70709806dd86"),
                Language = "en"
            });

        _chatbotClientMock
            .Setup(client => client.SendMessageAsync(
                It.IsAny<Guid>(),
                It.Is<string>(prompt => prompt.Contains("accounting journal entry", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<List<ChatbotAttachment>?>(),
                "application/json",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatbotMessageResponse
            {
                Content = """
                    {
                      "entry_type": "Income",
                      "date": "2026-05-18",
                      "description": "Stripe card payment",
                      "reference": "PAY-1008",
                      "amount": 129.95,
                      "currency_code": "USD",
                      "merchant_or_counterparty": "Stripe",
                      "confidence": 0.86,
                      "missing_fields": ["exchange rate"]
                    }
                    """
            });

        var result = await _controller.ExtractAccountingEntryFromDocument(new FormFileCollection(), "Stripe PAY-1008 USD 129.95", "Income");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var extracted = Assert.IsType<ExtractedAccountingEntryResponse>(ok.Value);
        Assert.Equal("Income", extracted.EntryType);
        Assert.Equal(new DateTime(2026, 5, 18), extracted.Date);
        Assert.Equal("Stripe card payment", extracted.Description);
        Assert.Equal("PAY-1008", extracted.Reference);
        Assert.Equal(129.95m, extracted.Amount);
        Assert.Equal("USD", extracted.CurrencyCode);
        Assert.Equal("Stripe", extracted.MerchantOrCounterparty);
        Assert.Equal("exchange rate", extracted.MissingFields[0]);
        Assert.True(extracted.Confidence > 0.8);
    }

    private static AiProcessingController CreateController(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://test")
        };

        var chatbotClientMock = new Mock<ChatbotServiceClient>(
            httpClient,
            new Mock<ILogger<ChatbotServiceClient>>().Object);

        return new AiProcessingController(
            chatbotClientMock.Object,
            new UploadServiceClient(httpClient),
            new RegistryServiceClient(httpClient),
            new CustomerServiceClient(httpClient, new Mock<ILogger<CustomerServiceClient>>().Object),
            new Mock<ILogger<AiProcessingController>>().Object);
    }

    private static string? ReadUrl(object? value)
    {
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return payload.RootElement.GetProperty("url").GetString();
    }
}

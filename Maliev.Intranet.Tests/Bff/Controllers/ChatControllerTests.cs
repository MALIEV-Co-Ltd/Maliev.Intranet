using System.Net;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Maliev.Intranet.Tests.Testing;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class ChatControllerTests
{
    private readonly Mock<ChatbotServiceClient> _chatbotClientMock;
    private readonly Mock<IChatContextResolver> _contextResolverMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<ChatHubService> _chatHubServiceMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IHostEnvironment> _hostEnvironmentMock;
    private readonly Mock<IChatCallbackTokenService> _callbackTokenServiceMock;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        _chatbotClientMock = new Mock<ChatbotServiceClient>(httpClient, new Mock<ILogger<ChatbotServiceClient>>().Object);
        _contextResolverMock = new Mock<IChatContextResolver>();

        var hubContextMock = new Mock<IHubContext<Maliev.Intranet.Bff.Hubs.ChatHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        hubContextMock.Setup(context => context.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(clients => clients.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _chatHubServiceMock = new Mock<ChatHubService>(hubContextMock.Object);

        _configMock = new Mock<IConfiguration>();
        _hostEnvironmentMock = new Mock<IHostEnvironment>();
        _hostEnvironmentMock
            .SetupGet(environment => environment.EnvironmentName)
            .Returns("Testing");
        _callbackTokenServiceMock = new Mock<IChatCallbackTokenService>();
        _callbackTokenServiceMock
            .Setup(service => service.CreateToken(It.IsAny<Guid>()))
            .Returns("callback-token");

        _controller = new ChatController(
            _chatbotClientMock.Object,
            _contextResolverMock.Object,
            _chatHubServiceMock.Object,
            _configMock.Object,
            _hostEnvironmentMock.Object,
            _callbackTokenServiceMock.Object);
    }

    [Fact]
    public async Task InitiateSession_ShouldReturnOk()
    {
        var response = new ChatbotSessionResponse { SessionId = Guid.NewGuid(), WelcomeMessage = "Hi" };
        _chatbotClientMock.Setup(x => x.InitiateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.InitiateSession(new BffChatSessionRequest(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bffResponse = Assert.IsType<BffChatSessionResponse>(okResult.Value);
        Assert.Equal(response.SessionId, bffResponse.SessionId);
    }

    [Fact]
    public async Task GetConversations_ShouldReturnEmployeeConversationSummaries()
    {
        var sessionId = Guid.NewGuid();
        var downstream = new ChatbotConversationListResponse
        {
            Data =
            [
                new ChatbotConversationSummary
                {
                    SessionId = sessionId,
                    Channel = "intranet",
                    Preview = "Can you create customer Acme?",
                    LastActivityAt = DateTimeOffset.Parse("2026-05-17T10:30:00Z"),
                    MessageCount = 2,
                    Status = "active"
                }
            ],
            Meta = new ChatbotPaginationMeta
            {
                Page = 1,
                PageSize = 20,
                TotalCount = 1
            }
        };

        _chatbotClientMock
            .Setup(x => x.GetConversationsAsync("intranet", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downstream);

        var result = await _controller.GetConversations("intranet", 1, 20, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bffResponse = Assert.IsType<BffChatConversationListResponse>(okResult.Value);
        Assert.Single(bffResponse.Data);
        Assert.Equal(sessionId, bffResponse.Data[0].SessionId);
        Assert.Equal("Can you create customer Acme?", bffResponse.Data[0].Preview);
        Assert.Equal(1, bffResponse.Meta.TotalCount);
    }

    [Fact]
    public async Task GetConversationMessages_ShouldReturnConversationMessages()
    {
        var sessionId = Guid.NewGuid();
        var downstream = new ChatbotConversationMessagesResponse
        {
            SessionId = sessionId,
            Channel = "intranet",
            Messages =
            [
                new ChatbotConversationMessage
                {
                    MessageId = Guid.NewGuid(),
                    Role = "user",
                    Content = "Can you create customer Acme?",
                    CreatedAt = DateTimeOffset.Parse("2026-05-17T10:30:00Z")
                },
                new ChatbotConversationMessage
                {
                    MessageId = Guid.NewGuid(),
                    Role = "assistant",
                    Content = "I can help with that.",
                    CreatedAt = DateTimeOffset.Parse("2026-05-17T10:31:00Z")
                }
            ]
        };

        _chatbotClientMock
            .Setup(x => x.GetConversationMessagesAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(downstream);

        var result = await _controller.GetConversationMessages(sessionId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bffResponse = Assert.IsType<BffChatConversationMessagesResponse>(okResult.Value);
        Assert.Equal(sessionId, bffResponse.SessionId);
        Assert.Equal(2, bffResponse.Messages.Count);
        Assert.Equal("user", bffResponse.Messages[0].Role);
        Assert.Equal("assistant", bffResponse.Messages[1].Role);
    }

    [Fact]
    public async Task GetInstructions_ShouldReturnInstructionProfiles()
    {
        var downstream = new List<BffSystemInstructionDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Customer Website Assistant",
                Category = BffSystemInstructionCategory.Core,
                TopicKey = "website",
                PersonaDefinition = "Mali website prompt",
                BusinessConstraints = "Customer-safe only",
                IsActive = true,
                Version = 2
            }
        };

        _chatbotClientMock
            .Setup(x => x.GetSystemInstructionsAsync(
                BffSystemInstructionCategory.Core,
                "website",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(downstream);

        var result = await _controller.GetInstructions(BffSystemInstructionCategory.Core, "website", true, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bffResponse = Assert.IsAssignableFrom<IReadOnlyList<BffSystemInstructionDto>>(okResult.Value);
        Assert.Single(bffResponse);
        Assert.Equal("website", bffResponse[0].TopicKey);
    }

    [Fact]
    public async Task UpdateInstruction_ShouldProxyWritablePromptMutation()
    {
        var id = Guid.NewGuid();
        var request = new BffSystemInstructionMutationRequest
        {
            Name = "Customer Website Assistant",
            Category = BffSystemInstructionCategory.Core,
            TopicKey = "website",
            PersonaDefinition = "Mali website prompt",
            BusinessConstraints = "Customer-safe only",
            IsActive = true
        };

        _chatbotClientMock
            .Setup(x => x.UpdateSystemInstructionAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BffSystemInstructionDto
            {
                Id = id,
                Name = request.Name,
                Category = request.Category,
                TopicKey = request.TopicKey,
                PersonaDefinition = request.PersonaDefinition,
                BusinessConstraints = request.BusinessConstraints,
                IsActive = true,
                Version = 3
            });

        var result = await _controller.UpdateInstruction(id, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bffResponse = Assert.IsType<BffSystemInstructionDto>(okResult.Value);
        Assert.Equal(id, bffResponse.Id);
        Assert.Equal("website", bffResponse.TopicKey);
    }

    [Fact]
    public async Task RefineInstruction_ShouldProxyWritablePromptDraft()
    {
        var request = new BffSystemInstructionRefinementRequest
        {
            Name = "Customer Website Assistant",
            Category = BffSystemInstructionCategory.Core,
            TopicKey = "website",
            PersonaDefinition = "Mali website prompt",
            BusinessConstraints = "Customer-safe only"
        };

        _chatbotClientMock
            .Setup(x => x.RefineSystemInstructionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BffSystemInstructionRefinementResponse
            {
                PersonaDefinition = "Refined Mali website prompt",
                BusinessConstraints = "Refined customer-safe constraints",
                Summary = "Clarified persona and tightened safety scope."
            });

        var result = await _controller.RefineInstruction(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bffResponse = Assert.IsType<BffSystemInstructionRefinementResponse>(okResult.Value);
        Assert.Contains("Refined", bffResponse.PersonaDefinition, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("safety", bffResponse.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnOk()
    {
        var request = new BffChatMessageRequest { SessionId = Guid.NewGuid(), Content = "hi" };
        var aiResponse = new ChatbotMessageResponse
        {
            Content = "Hello from AI",
            UiPings =
            [
                new ChatbotUiPing
                {
                    Target = "Artifacts",
                    Title = "3 artifacts added",
                    Detail = "Agent has added 3 artifacts.",
                    Count = 3
                }
            ]
        };

        _contextResolverMock.Setup(x => x.ResolveContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _chatbotClientMock.Setup(x => x.SendMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<List<ChatbotAttachment>>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResponse);

        var result = await _controller.SendMessage(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bffResponse = Assert.IsType<BffChatMessageResponse>(okResult.Value);
        Assert.Equal("Hello from AI", bffResponse.Content);
        Assert.Single(bffResponse.UiPings);
        Assert.Equal("Artifacts", bffResponse.UiPings[0].Target);
        Assert.Equal(3, bffResponse.UiPings[0].Count);
    }

    [Fact]
    public async Task SendMessageStream_AddsSessionCallbackTokenToCallbackUrl()
    {
        var sessionId = Guid.NewGuid();
        var request = new BffChatMessageRequest { SessionId = sessionId, Content = "hi" };
        var aiResponse = new ChatbotMessageResponse { Content = "Hello from AI" };

        _configMock
            .Setup(configuration => configuration["Services:IntranetBff:CallbackBaseUrl"])
            .Returns("https://intranet.maliev.com");

        _contextResolverMock
            .Setup(resolver => resolver.ResolveContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _chatbotClientMock
            .Setup(client => client.SendMessageStreamAsync(
                sessionId,
                It.IsAny<string>(),
                It.Is<string>(url =>
                    url.StartsWith($"https://intranet.maliev.com/api/v1/chat/callback/{sessionId}/thinking?", StringComparison.Ordinal) &&
                    url.Contains("token=callback-token", StringComparison.Ordinal)),
                It.IsAny<List<ChatbotAttachment>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResponse);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.SendMessageStream(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        _callbackTokenServiceMock.Verify(service => service.CreateToken(sessionId), Times.Once);
    }
}

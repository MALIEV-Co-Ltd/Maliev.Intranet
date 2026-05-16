using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for AI chat operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ChatController(
    ChatbotServiceClient chatbotClient,
    IChatContextResolver contextResolver,
    ChatHubService chatHubService,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    IChatCallbackTokenService callbackTokenService) : ControllerBase
{
    /// <summary>
    /// Initiates a new chat session.
    /// </summary>
    [RequirePermission(MalievPermissions.Chat.SessionsCreate, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("session")]
    public async Task<ActionResult<BffChatSessionResponse>> InitiateSession(
        [FromBody] BffChatSessionRequest request,
        CancellationToken ct)
    {
        var result = await chatbotClient.InitiateSessionAsync(
            request.Channel,
            request.Language,
            ct);

        if (result == null)
            return ChatbotFailure("Failed to initiate chat session.");

        return Ok(new BffChatSessionResponse
        {
            SessionId = result.SessionId,
            WelcomeMessage = result.WelcomeMessage,
            Language = result.Language,
            ExpiresAt = result.ExpiresAt
        });
    }

    /// <summary>
    /// Sends a message in an existing chat session.
    /// </summary>
    [RequirePermission(MalievPermissions.Chat.SessionsCreate, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("message")]
    public async Task<ActionResult<BffChatMessageResponse>> SendMessage(
        [FromBody] BffChatMessageRequest request,
        CancellationToken ct)
    {
        // Resolve page context and enrich the message
        var pageContext = await contextResolver.ResolveContextAsync(
            request.Content, request.Context, request.SessionId, ct);

        var enrichedContent = string.IsNullOrEmpty(pageContext)
            ? request.Content
            : $"{pageContext}\nUser message: {request.Content}";

        var attachments = request.Attachments?.Select(a => new ChatbotAttachment
        {
            Type = a.Type,
            Url = a.Url,
            MimeType = a.MimeType,
            SizeBytes = a.SizeBytes
        }).ToList();

        var result = await chatbotClient.SendMessageAsync(
            request.SessionId,
            enrichedContent,
            attachments,
            ct: ct);

        if (result == null)
            return ChatbotFailure("Failed to get AI response.");

        return Ok(new BffChatMessageResponse
        {
            MessageId = result.MessageId,
            Content = result.Content,
            Role = result.Role,
            SuggestedActions = result.SuggestedActions
                .Where(sa => !IsGenericAction(sa.Text))
                .Select(sa => new BffSuggestedAction
                {
                    Text = sa.Text,
                    Action = sa.Action,
                    Data = sa.Data
                }).ToList()
        });
    }

    /// <summary>
    /// Sends a message with streaming thinking steps via SignalR.
    /// Returns the final response with thinking steps included.
    /// </summary>
    [RequirePermission(MalievPermissions.Chat.SessionsCreate, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("message/stream")]
    public async Task<ActionResult<BffChatMessageResponse>> SendMessageStream(
        [FromBody] BffChatMessageRequest request,
        CancellationToken ct)
    {
        var pageContext = await contextResolver.ResolveContextAsync(
            request.Content, request.Context, request.SessionId, ct);

        var enrichedContent = string.IsNullOrEmpty(pageContext)
            ? request.Content
            : $"{pageContext}\nUser message: {request.Content}";

        var attachments = request.Attachments?.Select(a => new ChatbotAttachment
        {
            Type = a.Type,
            Url = a.Url,
            MimeType = a.MimeType,
            SizeBytes = a.SizeBytes
        }).ToList();

        // Build callback URL for thinking steps
        var callbackBaseUrl = configuration["Services:IntranetBff:CallbackBaseUrl"];
        if (string.IsNullOrEmpty(callbackBaseUrl))
        {
            callbackBaseUrl = $"{Request.Scheme}://{Request.Host}";
        }
        var callbackToken = callbackTokenService.CreateToken(request.SessionId);
        var callbackUrl = $"{callbackBaseUrl.TrimEnd('/')}/api/v1/chat/callback/{request.SessionId}/thinking?token={Uri.EscapeDataString(callbackToken)}";

        var result = await chatbotClient.SendMessageStreamAsync(
            request.SessionId,
            enrichedContent,
            callbackUrl,
            attachments,
            ct: ct);

        if (result == null)
            return ChatbotFailure("Failed to get AI response.");

        var response = new BffChatMessageResponse
        {
            MessageId = result.MessageId,
            Content = result.Content,
            Role = result.Role,
            SuggestedActions = result.SuggestedActions
                .Where(sa => !IsGenericAction(sa.Text))
                .Select(sa => new BffSuggestedAction
                {
                    Text = sa.Text,
                    Action = sa.Action,
                    Data = sa.Data
                }).ToList(),
            ThinkingSteps = result.ThinkingSteps?.Select(ts => new ThinkingStepDto
            {
                StepNumber = ts.StepNumber,
                Type = ts.Type,
                Title = ts.Title,
                Detail = ts.Detail,
                Timestamp = ts.Timestamp,
                DurationMs = ts.DurationMs
            }).ToList() ?? new()
        };

        // Also push the complete response via SignalR
        await chatHubService.SendMessageAsync(request.SessionId.ToString(), response);

        return Ok(response);
    }

    private static bool IsGenericAction(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        var genericTerms = new[] { "VIEW ALL SERVICES", "CONTACT US", "REQUEST A QUOTE" };
        return genericTerms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private ObjectResult ChatbotFailure(string message)
    {
        if (hostEnvironment.IsProduction() || string.IsNullOrWhiteSpace(chatbotClient.LastError))
        {
            return StatusCode(500, message);
        }

        return StatusCode(500, new
        {
            message,
            downstream = chatbotClient.LastError
        });
    }
}

using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for AI chat operations.
/// </summary>
/// <param name="chatbotClient">The chatbot service client.</param>
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
[ApiController]
[Route("api/[controller]")]
public class ChatController(ChatbotServiceClient chatbotClient) : ControllerBase
{
    /// <summary>
    /// Initiates a new chat session.
    /// </summary>
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
            return StatusCode(500, "Failed to initiate chat session.");

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
    [HttpPost("message")]
    public async Task<ActionResult<BffChatMessageResponse>> SendMessage(
        [FromBody] BffChatMessageRequest request,
        CancellationToken ct)
    {
        var attachments = request.Attachments?.Select(a => new ChatbotAttachment
        {
            Type = a.Type,
            Url = a.Url,
            MimeType = a.MimeType,
            SizeBytes = a.SizeBytes
        }).ToList();

        var result = await chatbotClient.SendMessageAsync(
            request.SessionId,
            request.Content,
            attachments,
            ct: ct);

        if (result == null)
            return StatusCode(500, "Failed to get AI response.");

        return Ok(new BffChatMessageResponse
        {
            MessageId = result.MessageId,
            Content = result.Content,
            Role = result.Role,
            SuggestedActions = result.SuggestedActions.Select(sa => new BffSuggestedAction
            {
                Text = sa.Text,
                Action = sa.Action,
                Data = sa.Data
            }).ToList()
        });
    }
}

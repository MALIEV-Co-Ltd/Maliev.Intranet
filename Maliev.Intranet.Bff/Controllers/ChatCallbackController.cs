using Asp.Versioning;
using Maliev.Intranet.Bff.Security;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Internal callback controller that receives thinking steps from ChatbotService
/// and pushes them to SignalR clients.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/chat/callback")]
[AllowAnonymous] // Callback authenticity is enforced with a short-lived per-session token.
public class ChatCallbackController(
    ChatHubService chatHubService,
    IChatCallbackTokenService callbackTokenService,
    ILogger<ChatCallbackController> logger) : ControllerBase
{
    /// <summary>
    /// Receives a thinking step from ChatbotService and pushes it to the session's SignalR group.
    /// </summary>
    [HttpPost("{sessionId}/thinking")]
    public async Task<IActionResult> ReceiveThinkingStep(Guid sessionId, [FromQuery] string? token, [FromBody] ThinkingStepDto step)
    {
        if (!callbackTokenService.IsValid(sessionId, token))
        {
            logger.LogWarning("Rejected chatbot thinking callback for session {SessionId}: invalid token.", sessionId);
            return Unauthorized();
        }

        logger.LogDebug("Received thinking step {StepNumber} for session {SessionId}: {Title}",
            step.StepNumber, sessionId, step.Title);

        await chatHubService.SendThinkingStepAsync(sessionId.ToString("D"), step);
        return Ok();
    }

    /// <summary>
    /// Receives the final response from ChatbotService and pushes it to the session's SignalR group.
    /// </summary>
    [HttpPost("{sessionId}/complete")]
    public async Task<IActionResult> ReceiveComplete(Guid sessionId, [FromQuery] string? token, [FromBody] BffChatMessageResponse response)
    {
        if (!callbackTokenService.IsValid(sessionId, token))
        {
            logger.LogWarning("Rejected chatbot completion callback for session {SessionId}: invalid token.", sessionId);
            return Unauthorized();
        }

        logger.LogDebug("Received complete response for session {SessionId}", sessionId);

        await chatHubService.SendMessageAsync(sessionId.ToString("D"), response);
        return Ok();
    }
}

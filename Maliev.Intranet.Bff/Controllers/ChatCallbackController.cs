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
[Route("api/chat/callback")]
[AllowAnonymous] // Internal service-to-service call
public class ChatCallbackController(ChatHubService chatHubService, ILogger<ChatCallbackController> logger) : ControllerBase
{
    /// <summary>
    /// Receives a thinking step from ChatbotService and pushes it to the session's SignalR group.
    /// </summary>
    [HttpPost("{sessionId}/thinking")]
    public async Task<IActionResult> ReceiveThinkingStep(string sessionId, [FromBody] ThinkingStepDto step)
    {
        logger.LogDebug("Received thinking step {StepNumber} for session {SessionId}: {Title}",
            step.StepNumber, sessionId, step.Title);

        await chatHubService.SendThinkingStepAsync(sessionId, step);
        return Ok();
    }

    /// <summary>
    /// Receives the final response from ChatbotService and pushes it to the session's SignalR group.
    /// </summary>
    [HttpPost("{sessionId}/complete")]
    public async Task<IActionResult> ReceiveComplete(string sessionId, [FromBody] BffChatMessageResponse response)
    {
        logger.LogDebug("Received complete response for session {SessionId}", sessionId);

        await chatHubService.SendMessageAsync(sessionId, response);
        return Ok();
    }
}

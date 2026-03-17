using Asp.Versioning;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for managing notification templates, delivery logs, and preferences.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/notifications")]
public class NotificationController(INotificationServiceClient notificationClient) : ControllerBase
{
    /// <summary>
    /// Gets notification templates.
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<PagedResponse<NotificationTemplateDto>>> GetTemplates([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? filter = null, CancellationToken ct = default)
    {
        var result = await notificationClient.GetTemplatesAsync(page, pageSize, filter, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<NotificationTemplateDto>());
    }

    /// <summary>
    /// Gets a notification template by ID.
    /// </summary>
    [HttpGet("templates/{id:guid}")]
    public async Task<ActionResult<NotificationTemplateDto>> GetTemplate(Guid id, CancellationToken ct)
    {
        var result = await notificationClient.GetTemplateByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Creates a notification template.
    /// </summary>
    [HttpPost("templates")]
    public async Task<ActionResult<NotificationTemplateDto>> CreateTemplate(CreateNotificationTemplateRequest request, CancellationToken ct)
    {
        var result = await notificationClient.CreateTemplateAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Updates a notification template.
    /// </summary>
    [HttpPut("templates/{id:guid}")]
    public async Task<ActionResult<NotificationTemplateDto>> UpdateTemplate(Guid id, UpdateNotificationTemplateRequest request, CancellationToken ct)
    {
        var result = await notificationClient.UpdateTemplateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a notification template.
    /// </summary>
    [HttpDelete("templates/{id:guid}")]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken ct)
    {
        await notificationClient.DeleteTemplateAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Gets delivery logs.
    /// </summary>
    [HttpGet("delivery-logs")]
    public async Task<ActionResult<PagedResponse<NotificationDeliveryLogDto>>> GetDeliveryLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? filter = null, CancellationToken ct = default)
    {
        var result = await notificationClient.GetDeliveryLogsAsync(page, pageSize, filter, ct);
        return result != null ? Ok(result) : Ok(new PagedResponse<NotificationDeliveryLogDto>());
    }

    /// <summary>
    /// Gets user notification preferences.
    /// </summary>
    [HttpGet("preferences/{userId}")]
    public async Task<ActionResult<UserNotificationPreferenceDto>> GetPreferences(string userId, CancellationToken ct)
    {
        var result = await notificationClient.GetPreferencesAsync(userId, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Updates user notification preferences.
    /// </summary>
    [HttpPut("preferences/{userId}")]
    public async Task<ActionResult<UserNotificationPreferenceDto>> UpdatePreferences(string userId, UpdateNotificationPreferenceRequest request, CancellationToken ct)
    {
        var result = await notificationClient.UpdatePreferencesAsync(userId, request, ct);
        return Ok(result);
    }
}

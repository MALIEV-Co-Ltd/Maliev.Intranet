using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// API controller for website contact request lifecycle management.
/// </summary>
/// <param name="contactClient">The ContactService client.</param>
/// <param name="notificationClient">The NotificationService client.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/contactrequests")]
public sealed class ContactRequestsController(
    IContactRequestServiceClient contactClient,
    INotificationServiceClient notificationClient) : ControllerBase
{
    /// <summary>
    /// Gets website contact requests.
    /// </summary>
    [RequirePermission(MalievPermissions.ContactRequest.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ContactRequestDto>>> GetAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ContactRequestStatus? status = null,
        [FromQuery] string? email = null,
        CancellationToken ct = default)
    {
        var result = await contactClient.GetContactRequestsAsync(page, pageSize, status, email, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a website contact request by id.
    /// </summary>
    [RequirePermission(MalievPermissions.ContactRequest.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContactRequestDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var result = await contactClient.GetContactRequestAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Updates a website contact request status or priority.
    /// </summary>
    [RequirePermission(MalievPermissions.ContactRequest.Update, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ContactRequestDto>> UpdateStatusAsync(
        int id,
        [FromBody] ContactRequestStatusUpdateRequest request,
        CancellationToken ct = default)
    {
        var result = await contactClient.UpdateContactStatusAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Sends a reply to the contact requester through NotificationService.
    /// </summary>
    [RequirePermission(MalievPermissions.ContactRequest.Update, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("{id:int}/reply")]
    public async Task<ActionResult<ContactRequestReplyResponse>> SendReplyAsync(
        int id,
        [FromBody] ContactRequestReplyRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ReplyMessage))
        {
            return BadRequest("Reply message is required.");
        }

        var contact = await contactClient.GetContactRequestAsync(id, ct);
        if (contact is null)
        {
            return NotFound();
        }

        await notificationClient.DispatchEventAsync(CreateReplyNotification(contact, request.ReplyMessage.Trim()), ct);

        var status = contact.Status;
        if (request.MarkResolved && status != ContactRequestStatus.Resolved)
        {
            var updated = await contactClient.UpdateContactStatusAsync(
                id,
                new ContactRequestStatusUpdateRequest
                {
                    Status = ContactRequestStatus.Resolved,
                    Priority = contact.Priority
                },
                ct);
            status = updated?.Status ?? ContactRequestStatus.Resolved;
        }

        return Ok(new ContactRequestReplyResponse
        {
            Sent = true,
            Status = status,
            Message = "Reply sent through NotificationService."
        });
    }

    /// <summary>
    /// Downloads a contact request attachment.
    /// </summary>
    [RequirePermission(MalievPermissions.ContactRequest.Read, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpGet("{id:int}/files/{fileId:int}/download")]
    public async Task<IActionResult> DownloadFileAsync(int id, int fileId, CancellationToken ct = default)
    {
        var file = await contactClient.DownloadContactFileAsync(id, fileId, ct);
        return file is null
            ? NotFound()
            : File(file.Content, file.ContentType, file.FileName);
    }

    private static NotificationEvent CreateReplyNotification(ContactRequestDto contact, string replyMessage)
    {
        return new NotificationEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "NotificationEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0",
            PublishedBy: "Maliev.Intranet",
            ConsumedBy: ["NotificationService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new NotificationEventPayload(
                NotificationType: "ContactMessageEmployeeReply",
                Priority: "normal",
                TargetUsers: [new NotificationEventPayloadTargetUsersItem($"contact-message-{contact.Id}-reply", "direct-email")],
                TemplateId: "contact-message-employee-reply",
                Parameters: new Dictionary<string, string>
                {
                    ["recipientEmail"] = contact.Email,
                    ["recipientName"] = string.IsNullOrWhiteSpace(contact.FullName) ? "MALIEV customer" : contact.FullName,
                    ["contactId"] = contact.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["subject"] = string.IsNullOrWhiteSpace(contact.Subject) ? contact.PublicReference : contact.Subject,
                    ["replyMessage"] = replyMessage
                },
                Metadata: new NotificationEventPayloadMetadata("en", "Maliev.Intranet")));
    }
}

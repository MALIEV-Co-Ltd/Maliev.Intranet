namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Processing status for a website contact request.
/// </summary>
public enum ContactRequestStatus
{
    /// <summary>New request waiting for review.</summary>
    New = 0,
    /// <summary>Request is being handled by MALIEV staff.</summary>
    InProgress = 1,
    /// <summary>Request has been resolved.</summary>
    Resolved = 2,
    /// <summary>Request is closed and archived.</summary>
    Closed = 3
}

/// <summary>
/// Priority level for a website contact request.
/// </summary>
public enum ContactRequestPriority
{
    /// <summary>Low priority.</summary>
    Low = 0,
    /// <summary>Medium priority.</summary>
    Medium = 1,
    /// <summary>High priority.</summary>
    High = 2,
    /// <summary>Urgent priority.</summary>
    Urgent = 3
}

/// <summary>
/// Website contact request category.
/// </summary>
public enum ContactRequestType
{
    /// <summary>General inquiry.</summary>
    General = 0,
    /// <summary>Supplier inquiry.</summary>
    Supplier = 1,
    /// <summary>Business partnership inquiry.</summary>
    Business = 3
}

/// <summary>
/// Website contact request shown in Maliev.Intranet.
/// </summary>
public sealed class ContactRequestDto
{
    /// <summary>Gets or sets the contact request id.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the public request reference.</summary>
    public string PublicReference { get; set; } = string.Empty;

    /// <summary>Gets or sets the sender full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the sender email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional phone number.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Gets or sets the optional company name.</summary>
    public string? Company { get; set; }

    /// <summary>Gets or sets the subject.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Gets or sets the message body.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the country id.</summary>
    public Guid CountryId { get; set; }

    /// <summary>Gets or sets the contact request type.</summary>
    public ContactRequestType ContactType { get; set; }

    /// <summary>Gets or sets the priority.</summary>
    public ContactRequestPriority Priority { get; set; }

    /// <summary>Gets or sets the lifecycle status.</summary>
    public ContactRequestStatus Status { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the optional resolution timestamp.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>Gets or sets the attached files.</summary>
    public List<ContactRequestFileDto> Files { get; set; } = [];
}

/// <summary>
/// Attachment metadata for a website contact request.
/// </summary>
public sealed class ContactRequestFileDto
{
    /// <summary>Gets or sets the file id.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the original file name.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the storage object name.</summary>
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long? FileSize { get; set; }

    /// <summary>Gets or sets the content type.</summary>
    public string? ContentType { get; set; }

    /// <summary>Gets or sets the upload service file id.</summary>
    public string? UploadServiceFileId { get; set; }

    /// <summary>Gets or sets the timestamp when the file was uploaded.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Request body for changing a website contact request status.
/// </summary>
public sealed class ContactRequestStatusUpdateRequest
{
    /// <summary>Gets or sets the new lifecycle status.</summary>
    public ContactRequestStatus Status { get; set; }

    /// <summary>Gets or sets the optional new priority.</summary>
    public ContactRequestPriority? Priority { get; set; }
}

/// <summary>
/// Request body for replying to a website contact request.
/// </summary>
public sealed class ContactRequestReplyRequest
{
    /// <summary>Gets or sets the employee reply message.</summary>
    public string ReplyMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the request should be marked resolved after sending.</summary>
    public bool MarkResolved { get; set; }
}

/// <summary>
/// Response returned after sending a website contact reply.
/// </summary>
public sealed class ContactRequestReplyResponse
{
    /// <summary>Gets or sets whether the reply notification was accepted.</summary>
    public bool Sent { get; set; }

    /// <summary>Gets or sets the resulting contact request status.</summary>
    public ContactRequestStatus Status { get; set; }

    /// <summary>Gets or sets the response message.</summary>
    public string Message { get; set; } = string.Empty;
}

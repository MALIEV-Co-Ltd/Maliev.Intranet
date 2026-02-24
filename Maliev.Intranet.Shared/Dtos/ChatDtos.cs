namespace Maliev.Intranet.Shared;

/// <summary>
/// Request to initiate a new chat session.
/// </summary>
public class BffChatSessionRequest
{
    /// <summary>Gets or sets the communication channel (e.g., intranet).</summary>
    public string Channel { get; set; } = "intranet";
    /// <summary>Gets or sets the language preference.</summary>
    public string Language { get; set; } = "en";
}

/// <summary>
/// Response from initiating a chat session.
/// </summary>
public class BffChatSessionResponse
{
    /// <summary>Gets or sets the session identifier.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Gets or sets the welcome message from the assistant.</summary>
    public string WelcomeMessage { get; set; } = string.Empty;
    /// <summary>Gets or sets the session language.</summary>
    public string Language { get; set; } = string.Empty;
    /// <summary>Gets or sets the expiration timestamp of the session.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// Request to send a chat message.
/// </summary>
public class BffChatMessageRequest
{
    /// <summary>Gets or sets the active session identifier.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Gets or sets the textual content of the message.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>Gets or sets optional context information.</summary>
    public string? Context { get; set; }
    /// <summary>Gets or sets optional attachments.</summary>
    public List<BffChatAttachment>? Attachments { get; set; }
}

/// <summary>
/// Response from sending a chat message.
/// </summary>
public class BffChatMessageResponse
{
    /// <summary>Gets or sets the unique message identifier.</summary>
    public Guid MessageId { get; set; }
    /// <summary>Gets or sets the response content.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>Gets or sets the role of the message sender (e.g., assistant).</summary>
    public string Role { get; set; } = "assistant";
    /// <summary>Gets or sets suggested follow-up actions.</summary>
    public List<BffSuggestedAction> SuggestedActions { get; set; } = new();
    /// <summary>Gets or sets the detailed steps of the assistant's reasoning process.</summary>
    public List<ThinkingStepDto> ThinkingSteps { get; set; } = new();
}

/// <summary>
/// Chat attachment for multimodal messages.
/// </summary>
public class BffChatAttachment
{
    /// <summary>Gets or sets the attachment type (e.g., image, pdf).</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Gets or sets the URL of the attachment.</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>Gets or sets the MIME type.</summary>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>Gets or sets the size in bytes.</summary>
    public long? SizeBytes { get; set; }
}

/// <summary>
/// Suggested action from AI response.
/// </summary>
public class BffSuggestedAction
{
    /// <summary>Gets or sets the display text for the action.</summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>Gets or sets the internal action code.</summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>Gets or sets additional data associated with the action.</summary>
    public string? Data { get; set; }
}

/// <summary>
/// A step in the AI's thinking/reasoning chain, shared between BFF and Client.
/// </summary>
public class ThinkingStepDto
{
    /// <summary>Gets or sets the sequential step number.</summary>
    public int StepNumber { get; set; }
    /// <summary>Gets or sets the type of thinking step.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Gets or sets the step title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the detailed description of the step.</summary>
    public string Detail { get; set; } = string.Empty;
    /// <summary>Gets or sets the timestamp when the step was recorded.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration of the step in milliseconds.</summary>
    public long? DurationMs { get; set; }
}

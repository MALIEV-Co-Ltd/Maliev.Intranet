namespace Maliev.Intranet.Shared;

/// <summary>
/// Request to initiate a new chat session.
/// </summary>
public class BffChatSessionRequest
{
    public string Channel { get; set; } = "intranet";
    public string Language { get; set; } = "en";
}

/// <summary>
/// Response from initiating a chat session.
/// </summary>
public class BffChatSessionResponse
{
    public Guid SessionId { get; set; }
    public string WelcomeMessage { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// Request to send a chat message.
/// </summary>
public class BffChatMessageRequest
{
    public Guid SessionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Context { get; set; }
    public List<BffChatAttachment>? Attachments { get; set; }
}

/// <summary>
/// Response from sending a chat message.
/// </summary>
public class BffChatMessageResponse
{
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = "assistant";
    public List<BffSuggestedAction> SuggestedActions { get; set; } = new();
    public List<ThinkingStepDto> ThinkingSteps { get; set; } = new();
}

/// <summary>
/// Chat attachment for multimodal messages.
/// </summary>
public class BffChatAttachment
{
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
}

/// <summary>
/// Suggested action from AI response.
/// </summary>
public class BffSuggestedAction
{
    public string Text { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Data { get; set; }
}

/// <summary>
/// A step in the AI's thinking/reasoning chain, shared between BFF and Client.
/// </summary>
public class ThinkingStepDto
{
    public int StepNumber { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public long? DurationMs { get; set; }
}

namespace Maliev.Intranet.Shared;

/// <summary>
/// Request payload to initiate a new AI-assisted chat session within the intranet.
/// </summary>
public class BffChatSessionRequest
{
    /// <summary>
    /// The communication channel for the chat session. Defaults to "intranet".
    /// </summary>
    public string Channel { get; set; } = "intranet";

    /// <summary>
    /// The preferred language for the chat session interactions. Defaults to "en".
    /// </summary>
    public string Language { get; set; } = "en";
}

/// <summary>
/// Response payload containing the initialized chat session details.
/// </summary>
public class BffChatSessionResponse
{
    /// <summary>
    /// The unique identifier of the newly created chat session.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// The initial greeting or welcome message from the assistant.
    /// </summary>
    public string WelcomeMessage { get; set; } = string.Empty;

    /// <summary>
    /// The language used for this session's responses.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp after which the chat session will expire.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// Request payload for sending a user message to an active chat session.
/// </summary>
public class BffChatMessageRequest
{
    /// <summary>
    /// The unique identifier of the active chat session.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// The textual content of the user's message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional context information to guide the AI's response.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Optional list of multimodal attachments associated with the message.
    /// </summary>
    public List<BffChatAttachment>? Attachments { get; set; }
}

/// <summary>
/// Response payload containing the assistant's reply and any suggested follow-up actions.
/// </summary>
public class BffChatMessageResponse
{
    /// <summary>
    /// The unique identifier assigned to the assistant's response message.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// The textual content of the assistant's response.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The role of the message sender, typically "assistant".
    /// </summary>
    public string Role { get; set; } = "assistant";

    /// <summary>
    /// A list of actions or queries suggested to the user based on the response.
    /// </summary>
    public List<BffSuggestedAction> SuggestedActions { get; set; } = new();

    /// <summary>
    /// A collection of internal steps describing the AI's reasoning process.
    /// </summary>
    public List<ThinkingStepDto> ThinkingSteps { get; set; } = new();
}

/// <summary>
/// Represents a file or media attachment associated with a chat message.
/// </summary>
public class BffChatAttachment
{
    /// <summary>
    /// The type of attachment (e.g., Image, Document).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The URI or URL where the attachment content can be accessed.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The media type (MIME) of the attachment.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// The size of the attachment in bytes, if available.
    /// </summary>
    public long? SizeBytes { get; set; }
}

/// <summary>
/// Represents a suggested action or interactive button presented to the user.
/// </summary>
public class BffSuggestedAction
{
    /// <summary>
    /// The display text for the suggested action button.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The internal action identifier to be executed when triggered.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Optional structured data payload associated with the action.
    /// </summary>
    public string? Data { get; set; }
}

/// <summary>
/// Data transfer object describing a specific step in the AI's thinking or reasoning chain.
/// </summary>
public class ThinkingStepDto
{
    /// <summary>
    /// The sequence number of this step in the overall chain.
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// The category of reasoning or action performed in this step.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// A brief title or header for the reasoning step.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the reasoning or internal operation.
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when this reasoning step was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// The duration of the reasoning step in milliseconds, if applicable.
    /// </summary>
    public long? DurationMs { get; set; }
}

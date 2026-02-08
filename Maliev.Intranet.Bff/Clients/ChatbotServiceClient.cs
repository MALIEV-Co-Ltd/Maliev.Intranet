using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the ChatbotService microservice.
/// ChatbotService uses snake_case JSON naming.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
/// <param name="logger">The logger instance.</param>
public class ChatbotServiceClient(HttpClient httpClient, ILogger<ChatbotServiceClient> logger)
{
    private static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Initiates a new chatbot session.
    /// </summary>
    public async Task<ChatbotSessionResponse?> InitiateSessionAsync(string channel, string language, CancellationToken ct = default)
    {
        var payload = new { channel, language };
        var content = JsonContent.Create(payload, options: SnakeCaseOptions);

        var response = await httpClient.PostAsync("/chatbot/v1/sessions/initiate", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("ChatbotService session initiation failed ({StatusCode}): {ErrorBody}", response.StatusCode, errorBody);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ChatbotSessionResponse>(SnakeCaseOptions, ct);
    }

    /// <summary>
    /// Sends a message to an existing chatbot session.
    /// </summary>
    public async Task<ChatbotMessageResponse?> SendMessageAsync(
        Guid sessionId,
        string content,
        List<ChatbotAttachment>? attachments = null,
        string? responseMimeType = null,
        object? responseSchema = null,
        CancellationToken ct = default)
    {
        var payload = new ChatbotSendMessageRequest
        {
            SessionId = sessionId,
            Content = content,
            Attachments = attachments,
            ResponseMimeType = responseMimeType,
            ResponseSchema = responseSchema
        };

        var jsonContent = JsonContent.Create(payload, options: SnakeCaseOptions);

        var response = await httpClient.PostAsync("/chatbot/v1/messages", jsonContent, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("ChatbotService send message failed ({StatusCode}): {ErrorBody}", response.StatusCode, errorBody);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ChatbotMessageResponse>(SnakeCaseOptions, ct);
    }

    /// <summary>
    /// Extracts customer data from documents or text via the ChatbotService extraction endpoint.
    /// </summary>
    /// <param name="storagePaths">Storage paths of uploaded files.</param>
    /// <param name="rawText">Optional raw text content.</param>
    /// <param name="files">Optional file data for multimodal extraction.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Extracted customer data, or null on failure.</returns>
    public async Task<ChatbotExtractCustomerResponse?> ExtractCustomerAsync(
        List<string> storagePaths, string? rawText, List<ChatbotExtractionFileData>? files = null, CancellationToken ct = default)
    {
        var payload = new ChatbotExtractCustomerRequest
        {
            StoragePaths = storagePaths,
            RawText = rawText,
            Files = files
        };
        var content = JsonContent.Create(payload, options: SnakeCaseOptions);

        var response = await httpClient.PostAsync("/chatbot/v1/extraction/customer", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("ChatbotService customer extraction failed ({StatusCode}): {ErrorBody}", response.StatusCode, errorBody);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ChatbotExtractCustomerResponse>(SnakeCaseOptions, ct);
    }
}

#region Internal DTOs matching ChatbotService snake_case API contract

internal class ChatbotExtractCustomerRequest
{
    public List<string> StoragePaths { get; set; } = [];
    public string? RawText { get; set; }
    public List<ChatbotExtractionFileData>? Files { get; set; }
}

/// <summary>
/// File data for multimodal extraction via ChatbotService.
/// </summary>
public class ChatbotExtractionFileData
{
    /// <summary>File name.</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Base64-encoded file content.</summary>
    public string Base64Data { get; set; } = string.Empty;
    /// <summary>MIME type of the file.</summary>
    public string MimeType { get; set; } = string.Empty;
}

internal class ChatbotSendMessageRequest
{
    public Guid SessionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<ChatbotAttachment>? Attachments { get; set; }
    public string? ResponseMimeType { get; set; }
    public object? ResponseSchema { get; set; }
}

/// <summary>
/// Attachment DTO for ChatbotService API.
/// </summary>
public class ChatbotAttachment
{
    /// <summary>Content type (Image, PDF, Video, Audio).</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>URL or data reference.</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>MIME type.</summary>
    public string MimeType { get; set; } = string.Empty;
    /// <summary>File size in bytes.</summary>
    public long? SizeBytes { get; set; }
}

/// <summary>
/// Response from ChatbotService session initiation.
/// </summary>
public class ChatbotSessionResponse
{
    /// <summary>Session identifier.</summary>
    public Guid SessionId { get; set; }
    /// <summary>Welcome message from the chatbot.</summary>
    public string WelcomeMessage { get; set; } = string.Empty;
    /// <summary>Language code.</summary>
    public string Language { get; set; } = string.Empty;
    /// <summary>Session expiration timestamp.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// Response from ChatbotService message send.
/// </summary>
public class ChatbotMessageResponse
{
    /// <summary>Message identifier.</summary>
    public Guid MessageId { get; set; }
    /// <summary>Response content.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>Message role (assistant/user).</summary>
    public string Role { get; set; } = string.Empty;
    /// <summary>Language code.</summary>
    public string Language { get; set; } = string.Empty;
    /// <summary>Suggested follow-up actions.</summary>
    public List<ChatbotSuggestedAction> SuggestedActions { get; set; } = new();
    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Suggested action from ChatbotService response.
/// </summary>
public class ChatbotSuggestedAction
{
    /// <summary>Display text.</summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>Action label.</summary>
    public string? Label { get; set; }
    /// <summary>Action type.</summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>Action data payload.</summary>
    public string? Data { get; set; }
}

/// <summary>
/// Response from ChatbotService customer extraction.
/// </summary>
public class ChatbotExtractCustomerResponse
{
    /// <summary>Extracted first name.</summary>
    public string? FirstName { get; set; }
    /// <summary>Extracted last name.</summary>
    public string? LastName { get; set; }
    /// <summary>Extracted email.</summary>
    public string? Email { get; set; }
    /// <summary>Extracted mobile phone.</summary>
    public string? Mobile { get; set; }
    /// <summary>Extracted landline phone.</summary>
    public string? Landline { get; set; }
    /// <summary>Extracted phone extension.</summary>
    public string? Extension { get; set; }
    /// <summary>Extracted segment.</summary>
    public string? Segment { get; set; }
    /// <summary>Extracted company name.</summary>
    public string? CompanyName { get; set; }
    /// <summary>Extracted company phone.</summary>
    public string? CompanyPhone { get; set; }
    /// <summary>Extracted VAT number.</summary>
    public string? VatNumber { get; set; }
    /// <summary>Extracted branch number (สาขาที่).</summary>
    public string? BranchNumber { get; set; }
    /// <summary>Extracted addresses.</summary>
    public List<ChatbotExtractedAddress>? Addresses { get; set; }
}

/// <summary>
/// An extracted address from ChatbotService.
/// </summary>
public class ChatbotExtractedAddress
{
    /// <summary>Address type (Billing or Shipping).</summary>
    public string? Type { get; set; }
    /// <summary>First address line.</summary>
    public string? AddressLine1 { get; set; }
    /// <summary>Second address line.</summary>
    public string? AddressLine2 { get; set; }
    /// <summary>Third address line.</summary>
    public string? AddressLine3 { get; set; }
    /// <summary>Sub-district.</summary>
    public string? District { get; set; }
    /// <summary>District/city.</summary>
    public string? City { get; set; }
    /// <summary>Province.</summary>
    public string? StateProvince { get; set; }
    /// <summary>Postal code.</summary>
    public string? PostalCode { get; set; }
    /// <summary>Shipping recipient name.</summary>
    public string? RecipientName { get; set; }
    /// <summary>Shipping recipient phone.</summary>
    public string? RecipientPhone { get; set; }
}

#endregion

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
    public virtual async Task<ChatbotSessionResponse?> InitiateSessionAsync(string channel, string language, CancellationToken ct = default)
    {
        try
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
        catch (OperationCanceledException)
        {
            logger.LogWarning("ChatbotService session initiation timed out.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ChatbotService session initiation failed unexpectedly.");
            return null;
        }
    }

    /// <summary>
    /// Sends a message to an existing chatbot session.
    /// </summary>
    public virtual async Task<ChatbotMessageResponse?> SendMessageAsync(
        Guid sessionId,
        string content,
        List<ChatbotAttachment>? attachments = null,
        string? responseMimeType = null,
        object? responseSchema = null,
        CancellationToken ct = default)
    {
        try
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
        catch (OperationCanceledException)
        {
            logger.LogWarning("ChatbotService send message timed out.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ChatbotService send message failed unexpectedly.");
            return null;
        }
    }

    /// <summary>
    /// Sends a message with a callback URL for streaming thinking steps.
    /// </summary>
    public virtual async Task<ChatbotMessageResponse?> SendMessageStreamAsync(
        Guid sessionId,
        string content,
        string callbackUrl,
        List<ChatbotAttachment>? attachments = null,
        CancellationToken ct = default)
    {
        try
        {
            var payload = new ChatbotSendMessageRequest
            {
                SessionId = sessionId,
                Content = content,
                Attachments = attachments,
                CallbackUrl = callbackUrl
            };

            var jsonContent = JsonContent.Create(payload, options: SnakeCaseOptions);

            var response = await httpClient.PostAsync("/chatbot/v1/messages", jsonContent, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("ChatbotService stream message failed ({StatusCode}): {ErrorBody}", response.StatusCode, errorBody);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ChatbotMessageResponse>(SnakeCaseOptions, ct);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("ChatbotService stream message timed out.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ChatbotService stream message failed unexpectedly.");
            return null;
        }
    }

    /// <summary>
    /// Extracts customer intent (needs customer data, search term, needs history) from a user message.
    /// </summary>
    public virtual async Task<ChatbotCustomerIntentResponse?> ExtractCustomerIntentAsync(string userMessage, CancellationToken ct = default)
    {
        try
        {
            var payload = new { user_message = userMessage };
            var content = JsonContent.Create(payload, options: SnakeCaseOptions);
            var response = await httpClient.PostAsync("/chatbot/v1/extraction/customer-intent", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("ChatbotService customer intent extraction failed ({StatusCode}): {ErrorBody}", response.StatusCode, errorBody);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<ChatbotCustomerIntentResponse>(SnakeCaseOptions, ct);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("ChatbotService customer intent extraction timed out.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ChatbotService customer intent extraction failed.");
            return null;
        }
    }

    /// <summary>
    /// Extracts customer data from documents or text via the ChatbotService extraction endpoint.
    /// </summary>
    /// <param name="storagePaths">Storage paths of uploaded files.</param>
    /// <param name="rawText">Optional raw text content.</param>
    /// <param name="files">Optional file data for multimodal extraction.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Extracted customer data, or null on failure.</returns>
    public virtual async Task<ChatbotExtractCustomerResponse?> ExtractCustomerAsync(
        List<string> storagePaths, string? rawText, List<ChatbotExtractionFileData>? files = null, CancellationToken ct = default)
    {
        try
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
        catch (OperationCanceledException)
        {
            logger.LogWarning("ChatbotService customer extraction timed out.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ChatbotService customer extraction failed unexpectedly.");
            return null;
        }
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
    public string? CallbackUrl { get; set; }
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
    /// <summary>Thinking steps from AI agent processing.</summary>
    public List<ChatbotThinkingStep> ThinkingSteps { get; set; } = new();
}

/// <summary>
/// Thinking step from ChatbotService response.
/// </summary>
public class ChatbotThinkingStep
{
    /// <summary>Step number.</summary>
    public int StepNumber { get; set; }
    /// <summary>Type of thinking step.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Title of thinking step.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Detail description of thinking step.</summary>
    public string Detail { get; set; } = string.Empty;
    /// <summary>Timestamp when step occurred.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Duration of step in milliseconds.</summary>
    public long? DurationMs { get; set; }
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
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }
    /// <summary>Extracted last name.</summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
    /// <summary>Extracted email.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    /// <summary>Extracted mobile phone.</summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }
    /// <summary>Extracted landline phone.</summary>
    [JsonPropertyName("landline")]
    public string? Landline { get; set; }
    /// <summary>Extracted phone extension.</summary>
    [JsonPropertyName("extension")]
    public string? Extension { get; set; }
    /// <summary>Extracted segment.</summary>
    [JsonPropertyName("segment")]
    public string? Segment { get; set; }
    /// <summary>Extracted company name.</summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }
    /// <summary>Extracted company phone.</summary>
    [JsonPropertyName("company_phone")]
    public string? CompanyPhone { get; set; }
    /// <summary>Extracted VAT number.</summary>
    [JsonPropertyName("vat_number")]
    public string? VatNumber { get; set; }
    /// <summary>Extracted branch number (สาขาที่).</summary>
    [JsonPropertyName("branch_number")]
    public string? BranchNumber { get; set; }
    /// <summary>Extracted addresses.</summary>
    [JsonPropertyName("addresses")]
    public List<ChatbotExtractedAddress>? Addresses { get; set; }
}

/// <summary>
/// An extracted address from ChatbotService.
/// </summary>
public class ChatbotExtractedAddress
{
    /// <summary>Address type (Billing or Shipping).</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    /// <summary>First address line.</summary>
    [JsonPropertyName("address_line_1")]
    public string? AddressLine1 { get; set; }
    /// <summary>Second address line.</summary>
    [JsonPropertyName("address_line_2")]
    public string? AddressLine2 { get; set; }
    /// <summary>Third address line.</summary>
    [JsonPropertyName("address_line_3")]
    public string? AddressLine3 { get; set; }
    /// <summary>Sub-district.</summary>
    [JsonPropertyName("district")]
    public string? District { get; set; }
    /// <summary>District/city.</summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }
    /// <summary>Province.</summary>
    [JsonPropertyName("state_province")]
    public string? StateProvince { get; set; }
    /// <summary>Postal code.</summary>
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }
    /// <summary>Shipping recipient name.</summary>
    [JsonPropertyName("recipient_name")]
    public string? RecipientName { get; set; }
    /// <summary>Shipping recipient phone.</summary>
    [JsonPropertyName("recipient_phone")]
    public string? RecipientPhone { get; set; }
}

/// <summary>
/// Response from ChatbotService customer intent extraction.
/// </summary>
public class ChatbotCustomerIntentResponse
{
    /// <summary>Whether the user needs customer data.</summary>
    public bool NeedsCustomerData { get; set; }
    /// <summary>Customer search term extracted from message.</summary>
    public string? CustomerSearchTerm { get; set; }
    /// <summary>Whether the user needs activity history.</summary>
    public bool NeedsHistory { get; set; }
}

#endregion

using System.Text;
using System.Text.RegularExpressions;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Service for resolving context (like customer data) for chat messages based on the current page and message content.
/// </summary>
public interface IChatContextResolver
{
    /// <summary>
    /// Resolves the context for a chat message.
    /// </summary>
    /// <param name="userMessage">The user's message content.</param>
    /// <param name="contextUrl">The URL of the page the user is currently on.</param>
    /// <param name="chatSessionId">The current chat session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A formatted context string to be prepended to the user's message, or null if no context is needed.</returns>
    Task<string?> ResolveContextAsync(string userMessage, string? contextUrl, Guid chatSessionId, CancellationToken ct);
}

/// <summary>
/// Implementation of <see cref="IChatContextResolver"/> that enriches chat messages with customer data.
/// </summary>
/// <param name="chatbotClient">The chatbot service client.</param>
/// <param name="customerClient">The customer service client.</param>
/// <param name="logger">The logger instance.</param>
public class ChatContextResolver(
    ChatbotServiceClient chatbotClient,
    CustomerServiceClient customerClient,
    ILogger<ChatContextResolver> logger) : IChatContextResolver
{
    private static readonly Regex CustomerDetailPattern = new(
        @"(?:/sales)?/customers/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <inheritdoc />
    public async Task<string?> ResolveContextAsync(string userMessage, string? contextUrl, Guid chatSessionId, CancellationToken ct)
    {
        try
        {
            // 1. Try to extract customer ID from URL
            Guid? customerIdFromUrl = null;
            if (!string.IsNullOrEmpty(contextUrl))
            {
                var match = CustomerDetailPattern.Match(contextUrl);
                if (match.Success && Guid.TryParse(match.Groups[1].Value, out var guid))
                {
                    customerIdFromUrl = guid;
                }
            }

            // 2. Extract intent and entities from user message
            var intent = await ExtractIntentAsync(userMessage, chatSessionId, ct);
            if (intent == null || !intent.NeedsCustomerData)
            {
                return null;
            }

            // 3. Resolve the target customer
            Guid? targetCustomerId = null;
            string? searchSummary = null;

            if (!string.IsNullOrEmpty(intent.CustomerSearchTerm))
            {
                // Search by term
                var searchResults = await customerClient.GetCustomersAsync(intent.CustomerSearchTerm, page: 1, ct: ct);
                var resultsList = searchResults?.Data.ToList() ?? [];

                if (resultsList.Count == 1)
                {
                    targetCustomerId = resultsList[0].Id;
                }
                else if (resultsList.Count > 1)
                {
                    searchSummary = $"Multiple customers found for '{intent.CustomerSearchTerm}': " +
                                    string.Join(", ", resultsList.Take(3).Select(c => $"{c.Name} ({c.Email})")) +
                                    (resultsList.Count > 3 ? "..." : "");
                }
            }
            else if (customerIdFromUrl.HasValue)
            {
                // Use current page context
                targetCustomerId = customerIdFromUrl;
            }

            if (!targetCustomerId.HasValue)
            {
                return searchSummary != null ? $"[SYSTEM CONTEXT: {searchSummary}]" : null;
            }

            // 4. Fetch full customer details
            var customer = await customerClient.GetCustomerByIdAsync(targetCustomerId.Value, ct);
            if (customer == null) return null;

            // 5. Optionally fetch activity history
            PagedResponse<CustomerActivityResponse>? history = null;
            if (intent.NeedsHistory)
            {
                history = await customerClient.GetCustomerActivityAsync(targetCustomerId.Value, page: 1, pageSize: 20, ct: ct);
            }

            // 6. Format context
            return FormatCustomerContext(customer, history?.Data?.ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resolving chat context");
            return null;
        }
    }

    private async Task<IntentExtractionResult?> ExtractIntentAsync(string userMessage, Guid chatSessionId, CancellationToken ct)
    {
        var response = await chatbotClient.ExtractCustomerIntentAsync(userMessage, ct);
        if (response == null) return null;

        return new IntentExtractionResult(
            response.NeedsCustomerData,
            response.CustomerSearchTerm,
            response.NeedsHistory);
    }

    private string FormatCustomerContext(CustomerDetailDto customer, List<CustomerActivityResponse>? history)
    {
        var sb = new StringBuilder();
        sb.AppendLine("### SYSTEM CONTEXT: REAL-TIME CUSTOMER DATA ###");
        sb.AppendLine("Use ONLY the data provided below. If data is missing, state that clearly.");
        sb.AppendLine();
        sb.AppendLine($"[CUSTOMER PROFILE]");
        sb.AppendLine($"Name: {customer.Name}");
        sb.AppendLine($"ID: {customer.Id}");
        sb.AppendLine($"Email: {customer.Email}");
        if (!string.IsNullOrEmpty(customer.Mobile)) sb.AppendLine($"Mobile: {customer.Mobile}");
        sb.AppendLine($"Status: {customer.Status}");
        sb.AppendLine($"Segment: {customer.Segment}");
        sb.AppendLine($"Tier: {customer.Tier}");

        if (!string.IsNullOrEmpty(customer.CompanyName))
        {
            sb.AppendLine($"Company: {customer.CompanyName}");
            sb.AppendLine($"VAT Number: {customer.CompanyVatNumber ?? "Not provided"}");
        }

        var activeNda = customer.Ndas.OrderByDescending(n => n.CreatedAt).FirstOrDefault(n => n.Status == "Signed");
        sb.AppendLine($"NDA Status: {(activeNda != null ? $"Signed (Expires: {activeNda.ExpiresAt?.ToString("yyyy-MM-dd") ?? "Never"})" : "No active NDA found")}");

        if (customer.Documents.Count > 0)
        {
            sb.AppendLine($"Documents: {string.Join(", ", customer.Documents.Take(10).Select(d => d.FileName))}");
        }

        var billingAddr = customer.Addresses.FirstOrDefault(a => a.Type == "Billing" && a.IsDefault) ?? customer.Addresses.FirstOrDefault(a => a.Type == "Billing");
        if (billingAddr != null)
        {
            sb.AppendLine($"Billing Address: {billingAddr.AddressLine1}, {billingAddr.District ?? ""}, {billingAddr.City}, {billingAddr.StateProvince} {billingAddr.PostalCode}");
        }
        else
        {
            sb.AppendLine("Billing Address: No billing address on file.");
        }

        var shippingAddr = customer.Addresses.FirstOrDefault(a => a.Type == "Shipping" && a.IsDefault) ?? customer.Addresses.FirstOrDefault(a => a.Type == "Shipping");
        if (shippingAddr != null)
        {
            sb.AppendLine($"Shipping Address: {shippingAddr.RecipientName} | {shippingAddr.AddressLine1}, {shippingAddr.District ?? ""}, {shippingAddr.City}, {shippingAddr.StateProvince} {shippingAddr.PostalCode} | Phone: {shippingAddr.RecipientPhone ?? "N/A"}");
        }

        if (customer.Notes.Count > 0)
        {
            sb.AppendLine("\n[INTERNAL NOTES]");
            foreach (var note in customer.Notes.OrderByDescending(n => n.CreatedAt).Take(3))
            {
                var text = note.NoteText.Length > 200 ? note.NoteText[..197] + "..." : note.NoteText;
                sb.AppendLine($"- [{note.CreatedAt:yyyy-MM-dd}] {text}");
            }
        }

        if (history != null && history.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[ACTIVITY HISTORY]");
            foreach (var item in history.OrderByDescending(h => h.Timestamp).Take(15))
            {
                sb.AppendLine($"- {item.Timestamp:yyyy-MM-dd HH:mm} | {item.Action} | {item.Description} | by {item.ActorName}");
            }
        }

        sb.AppendLine("\n### END SYSTEM CONTEXT ###");
        return sb.ToString();
    }

    private record IntentExtractionResult(
        [property: System.Text.Json.Serialization.JsonPropertyName("needs_customer_data")] bool NeedsCustomerData,
        [property: System.Text.Json.Serialization.JsonPropertyName("customer_search_term")] string? CustomerSearchTerm,
        [property: System.Text.Json.Serialization.JsonPropertyName("needs_history")] bool NeedsHistory);
}

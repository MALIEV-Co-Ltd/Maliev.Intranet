using System.Text.Json;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the PdfService.
/// </summary>
public class PdfServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Generates a PDF document synchronously.
    /// </summary>
    /// <param name="documentType">The type of document to generate.</param>
    /// <param name="referenceId">The business reference ID (e.g., InvoiceId).</param>
    /// <param name="data">The data payload for the PDF template.</param>
    /// <param name="templateCode">Optional template code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated PDF URL.</returns>
    public async Task<string?> GeneratePdfAsync(
        PdfDocumentType documentType,
        string referenceId,
        object data,
        string? templateCode = null,
        CancellationToken ct = default)
    {
        var documentTypeEnum = documentType switch
        {
            PdfDocumentType.Quotation => "Quotation",
            PdfDocumentType.Invoice => "Invoice",
            PdfDocumentType.Receipt => "Receipt",
            PdfDocumentType.Report => "Report",
            PdfDocumentType.DeliveryNote => "DeliveryNote",
            _ => documentType.ToString()
        };

        var request = new
        {
            templateCode = templateCode ?? documentTypeEnum,
            referenceId = referenceId,
            documentType = documentTypeEnum,
            data = data
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await httpClient.PostAsync("/pdf/v1.0/generations/generate", content, ct);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (result.TryGetProperty("storageUrl", out var url))
            {
                return url.GetString();
            }
        }

        return null;
    }
}

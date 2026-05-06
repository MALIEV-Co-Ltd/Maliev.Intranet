using System.Text.Json;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the PdfService.
/// </summary>
public class PdfServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Gets the latest completed PDF URL for a business reference.
    /// </summary>
    /// <param name="documentType">The document type.</param>
    /// <param name="referenceId">The stable business reference ID used for PDF generation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The latest generated PDF URL, if one exists.</returns>
    public async Task<string?> GetLatestPdfUrlAsync(
        PdfDocumentType documentType,
        string referenceId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(referenceId))
            return null;

        var documentTypeValue = documentType switch
        {
            PdfDocumentType.Quotation => "Quotation",
            PdfDocumentType.Invoice => "Invoice",
            PdfDocumentType.Receipt => "Receipt",
            PdfDocumentType.Report => "Report",
            PdfDocumentType.DeliveryNote => "DeliveryNote",
            _ => documentType.ToString()
        };

        var url = $"/pdf/v1/generations/latest?documentType={Uri.EscapeDataString(documentTypeValue)}&referenceId={Uri.EscapeDataString(referenceId)}";
        var response = await httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return result.TryGetProperty("storageUrl", out var storageUrl) && storageUrl.ValueKind == JsonValueKind.String
            ? storageUrl.GetString()
            : null;
    }

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

        var response = await httpClient.PostAsync("/pdf/v1/generations/generate", content, ct);

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

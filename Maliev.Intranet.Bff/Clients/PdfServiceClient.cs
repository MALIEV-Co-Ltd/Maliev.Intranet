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
            PdfDocumentType.CommerceBom => "CommerceBom",
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
        var artifact = await GeneratePdfArtifactAsync(
            documentType,
            referenceId,
            data,
            templateCode,
            ct);

        return artifact?.StorageUrl;
    }

    /// <summary>
    /// Generates a PDF document synchronously and returns URL plus durable storage-path metadata when provided.
    /// </summary>
    /// <param name="documentType">The type of document to generate.</param>
    /// <param name="referenceId">The business reference ID.</param>
    /// <param name="data">The data payload for the PDF template.</param>
    /// <param name="templateCode">Optional template code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated PDF artifact metadata.</returns>
    public async Task<PdfGenerationResult?> GeneratePdfArtifactAsync(
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
            PdfDocumentType.CommerceBom => "CommerceBom",
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
            var storageUrl = ReadString(result, "storageUrl", "pdfUrl", "url");
            if (string.IsNullOrWhiteSpace(storageUrl))
                return null;

            var storagePath = ReadString(result, "storagePath", "pdfArtifactStoragePath");
            if (string.IsNullOrWhiteSpace(storagePath) && LooksLikeStoragePath(storageUrl))
                storagePath = storageUrl;

            return new PdfGenerationResult
            {
                StorageUrl = storageUrl,
                StoragePath = storagePath
            };
        }

        return null;
    }

    private static string? ReadString(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString();

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
        }

        return null;
    }

    private static bool LooksLikeStoragePath(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !Uri.TryCreate(value, UriKind.Absolute, out _) &&
        value.Contains('/', StringComparison.Ordinal);
}

/// <summary>
/// Metadata returned after PdfService generates a document.
/// </summary>
public sealed class PdfGenerationResult
{
    /// <summary>The browser-accessible URL returned by PdfService.</summary>
    public string StorageUrl { get; set; } = string.Empty;

    /// <summary>The durable UploadService storage path, when PdfService returns one.</summary>
    public string? StoragePath { get; set; }
}

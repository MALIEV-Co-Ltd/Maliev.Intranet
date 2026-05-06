using System.Security.Claims;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Services;

internal static class QuotationPdfMetadataApplicator
{
    public static void Apply(QuotationPdfData pdfData, ClaimsPrincipal? user, DateTime? quotedAt = null)
    {
        pdfData.QuotedByName = FirstNonEmpty(
            user?.Identity?.Name,
            user?.FindFirst("name")?.Value,
            user?.FindFirst("preferred_username")?.Value,
            user?.FindFirst("email")?.Value,
            user?.FindFirst(ClaimTypes.Email)?.Value,
            pdfData.QuotedByName);

        pdfData.QuotedByEmail = FirstNonEmpty(
            user?.FindFirst("email")?.Value,
            user?.FindFirst(ClaimTypes.Email)?.Value,
            pdfData.QuotedByEmail);

        pdfData.QuotedAt = quotedAt ?? DateTime.UtcNow;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

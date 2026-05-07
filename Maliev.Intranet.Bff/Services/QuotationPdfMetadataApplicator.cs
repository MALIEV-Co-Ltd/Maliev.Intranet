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

        pdfData.QuotedByPhone = FirstNonEmpty(
            user?.FindFirst("phone_number")?.Value,
            user?.FindFirst("phone")?.Value,
            user?.FindFirst("mobile_phone")?.Value,
            user?.FindFirst("mobile")?.Value,
            user?.FindFirst("telephone_number")?.Value,
            user?.FindFirst(ClaimTypes.MobilePhone)?.Value,
            user?.FindFirst(ClaimTypes.HomePhone)?.Value,
            user?.FindFirst(ClaimTypes.OtherPhone)?.Value,
            pdfData.QuotedByPhone);

        pdfData.QuotedAt = quotedAt ?? DateTime.UtcNow;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

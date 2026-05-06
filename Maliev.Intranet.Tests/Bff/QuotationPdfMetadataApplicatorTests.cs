using System.Security.Claims;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Tests.Bff;

/// <summary>
/// Tests quotation PDF employee metadata stamping.
/// </summary>
public class QuotationPdfMetadataApplicatorTests
{
    /// <summary>
    /// Verifies quotation PDF metadata uses the authenticated employee identity.
    /// </summary>
    [Fact]
    public void Apply_UsesAuthenticatedEmployeeIdentity()
    {
        var pdfData = new QuotationPdfData
        {
            QuotedByName = "MALIEV"
        };
        var quotedAt = new DateTime(2026, 5, 6, 14, 30, 0, DateTimeKind.Utc);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("name", "Natthapol Vanasrivilai"),
            new Claim(ClaimTypes.Email, "natthapol.vanasrivilai@maliev.com")
        ], "Test"));

        QuotationPdfMetadataApplicator.Apply(pdfData, user, quotedAt);

        Assert.Equal("Natthapol Vanasrivilai", pdfData.QuotedByName);
        Assert.Equal("natthapol.vanasrivilai@maliev.com", pdfData.QuotedByEmail);
        Assert.Equal(quotedAt, pdfData.QuotedAt);
    }
}

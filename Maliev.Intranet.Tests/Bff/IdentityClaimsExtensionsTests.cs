using System.Security.Claims;
using Maliev.Intranet.Bff;
using Xunit;

namespace Maliev.Intranet.Tests.Bff;

public class IdentityClaimsExtensionsTests
{
    [Fact]
    public void GetProfileImageUrl_UsesGoogleSpecificClaim_WhenStandardClaimMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("urn:google:picture", "https://lh3.googleusercontent.com/a/test-user")
        }));

        var imageUrl = principal.GetProfileImageUrl();

        Assert.Equal("https://lh3.googleusercontent.com/a/test-user", imageUrl);
    }

    [Fact]
    public void GetGoogleUserId_PrefersGoogleClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("google_user_id", "google-123"),
            new Claim(ClaimTypes.NameIdentifier, "platform-456")
        }));

        var googleUserId = principal.GetGoogleUserId();

        Assert.Equal("google-123", googleUserId);
    }
}

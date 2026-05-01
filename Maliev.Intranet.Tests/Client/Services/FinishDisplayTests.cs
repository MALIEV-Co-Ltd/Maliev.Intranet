using Maliev.Intranet.Client.Services;

namespace Maliev.Intranet.Tests.Client.Services;

public class FinishDisplayTests
{
    [Theory]
    [InlineData("AS_PRINTED", "As printed")]
    [InlineData("BEAD_BLAST", "Bead blast")]
    [InlineData("ANODIZED", "Anodized")]
    [InlineData("CNC_MILL", "Cnc mill")]
    [InlineData("FDM", "Fdm")]
    public void Humanize_TransformsCodeToReadableText(string code, string expected)
    {
        Assert.Equal(expected, FinishDisplay.Humanize(code));
    }

    [Fact]
    public void Humanize_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, FinishDisplay.Humanize(""));
    }

    [Fact]
    public void Humanize_Null_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, FinishDisplay.Humanize(null));
    }

    [Fact]
    public void Humanize_Whitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, FinishDisplay.Humanize("   "));
    }
}

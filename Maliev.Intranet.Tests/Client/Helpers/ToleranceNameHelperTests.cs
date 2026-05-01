using Maliev.Intranet.Client.Helpers;
using Xunit;

namespace Maliev.Intranet.Tests.Client.Helpers;

public class ToleranceNameHelperTests
{
    [Theory]
    [InlineData("Fine (ISO 2768-f)", "Fine", "ISO 2768-f")]
    [InlineData("Coarse (ISO 2768-c)", "Coarse", "ISO 2768-c")]
    [InlineData("Tight (±0.05)", "Tight", "±0.05")]
    [InlineData("Standard", "Standard", "")]
    [InlineData("Medium (ISO 2768-m)", "Medium", "ISO 2768-m")]
    public void Split_ReturnsExpectedHeadAndTail(string input, string expectedHead, string expectedTail)
    {
        var (head, tail) = ToleranceNameHelper.Split(input);

        Assert.Equal(expectedHead, head);
        Assert.Equal(expectedTail, tail);
    }

    [Fact]
    public void Split_NoParentheses_ReturnsFull_EmptyTail()
    {
        var (head, tail) = ToleranceNameHelper.Split("Custom Tolerance");

        Assert.Equal("Custom Tolerance", head);
        Assert.Equal("", tail);
    }

    [Fact]
    public void Split_UnclosedParenthesis_ReturnsContentAfterOpen()
    {
        var (head, tail) = ToleranceNameHelper.Split("Fine (ISO 2768-f");

        Assert.Equal("Fine", head);
        Assert.Equal("ISO 2768-f", tail);
    }
}

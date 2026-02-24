using Maliev.Intranet.Shared.Services;

namespace Maliev.Intranet.Tests.Shared;

/// <summary>Tests for the MarkdownService HTML conversion and sanitization behavior.</summary>
public class MarkdownServiceTests
{
    private readonly MarkdownService _service = new();

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    /// <summary>Verifies that an empty string is returned when the markdown input is null, empty, or whitespace.</summary>
    public void ToHtml_ShouldReturnEmpty_WhenInputIsEmpty(string? input, string expected)
    {
        var result = _service.ToHtml(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    /// <summary>Verifies that basic markdown syntax is converted to the expected HTML output.</summary>
    public void ToHtml_ShouldRenderBasicMarkdown()
    {
        var markdown = @"# Hello
**bold**";
        var result = _service.ToHtml(markdown);

        // Markdig with AdvancedExtensions adds ID to headers
        Assert.Contains("Hello", result);
        Assert.Contains("<h1", result);
        Assert.Contains("<strong>bold</strong>", result);
    }

    [Fact]
    /// <summary>Verifies that cross-site scripting payloads are sanitized from the rendered HTML output.</summary>
    public void ToHtml_ShouldSanitizeXss()
    {
        var markdown = @"[Click me](javascript:alert('xss')) <script>alert('xss')</script> <div style=""color:red"">test</div>";
        var result = _service.ToHtml(markdown);

        Assert.DoesNotContain("javascript:", result);
        Assert.DoesNotContain("<script>", result);
        Assert.DoesNotContain(@"style=""color:red""", result);
        Assert.Contains("test", result);
    }

    [Fact]
    /// <summary>Verifies that markdown table syntax is rendered as an HTML table.</summary>
    public void ToHtml_ShouldRenderTables()
    {
        var markdown = @"| Header 1 | Header 2 |
| --- | --- |
| Cell 1 | Cell 2 |";
        var result = _service.ToHtml(markdown);

        Assert.Contains("<table>", result);
        Assert.Contains("<thead>", result);
        Assert.Contains("<tbody>", result);
        Assert.Contains("<td>Cell 1</td>", result);
    }

    [Fact]
    /// <summary>Verifies that markdown list syntax is rendered as an HTML unordered list.</summary>
    public void ToHtml_ShouldRenderLists()
    {
        var markdown = @"* Item 1
* Item 2";
        var result = _service.ToHtml(markdown);
        Assert.Contains("<ul>", result);
        Assert.Contains("<li>Item 1</li>", result);
    }

    [Fact]
    /// <summary>Verifies that markdown blockquote syntax is rendered as an HTML blockquote element.</summary>
    public void ToHtml_ShouldRenderQuotes()
    {
        var markdown = "> Quote";
        var result = _service.ToHtml(markdown);
        Assert.Contains("<blockquote>", result);
        Assert.Contains("Quote", result);
    }

    [Fact]
    /// <summary>Verifies that markdown link syntax is rendered as an HTML anchor element.</summary>
    public void ToHtml_ShouldRenderLinks()
    {
        var markdown = "[Link](http://example.com)";
        var result = _service.ToHtml(markdown);
        Assert.Contains(@"<a href=""http://example.com"">Link</a>", result);
    }
}

using Ganss.Xss;
using Markdig;

namespace Maliev.Intranet.Shared.Services;

/// <summary>
/// Service for rendering Markdown to sanitized HTML using Markdig and HtmlSanitizer.
/// </summary>
public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly HtmlSanitizer _sanitizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownService"/> class.
    /// </summary>
    public MarkdownService()
    {
        // Configure Markdig pipeline with common extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();

        // Configure HtmlSanitizer with safe defaults
        _sanitizer = new HtmlSanitizer();

        // Allow common safe tags for markdown content
        _sanitizer.AllowedTags.Add("h1");
        _sanitizer.AllowedTags.Add("h2");
        _sanitizer.AllowedTags.Add("h3");
        _sanitizer.AllowedTags.Add("h4");
        _sanitizer.AllowedTags.Add("h5");
        _sanitizer.AllowedTags.Add("h6");
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("br");
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("em");
        _sanitizer.AllowedTags.Add("ul");
        _sanitizer.AllowedTags.Add("ol");
        _sanitizer.AllowedTags.Add("li");
        _sanitizer.AllowedTags.Add("a");
        _sanitizer.AllowedTags.Add("code");
        _sanitizer.AllowedTags.Add("pre");
        _sanitizer.AllowedTags.Add("blockquote");
        _sanitizer.AllowedTags.Add("hr");
        _sanitizer.AllowedTags.Add("table");
        _sanitizer.AllowedTags.Add("thead");
        _sanitizer.AllowedTags.Add("tbody");
        _sanitizer.AllowedTags.Add("tr");
        _sanitizer.AllowedTags.Add("th");
        _sanitizer.AllowedTags.Add("td");

        // Allow safe attributes only (NO style attribute for security)
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("id");
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedAttributes.Add("rel");

        // Explicitly deny dangerous attributes
        _sanitizer.AllowedAttributes.Remove("style");
        _sanitizer.AllowedAttributes.Remove("onclick");
        _sanitizer.AllowedAttributes.Remove("onload");
        _sanitizer.AllowedAttributes.Remove("onerror");
    }

    /// <inheritdoc />
    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        // Convert Markdown to HTML
        var html = Markdown.ToHtml(markdown, _pipeline);

        // Sanitize HTML to prevent XSS
        var sanitizedHtml = _sanitizer.Sanitize(html);

        return sanitizedHtml;
    }
}

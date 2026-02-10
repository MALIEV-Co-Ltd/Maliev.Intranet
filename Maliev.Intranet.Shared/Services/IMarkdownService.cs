namespace Maliev.Intranet.Shared.Services;

/// <summary>
/// Service for rendering Markdown to sanitized HTML.
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// Converts Markdown to sanitized HTML.
    /// </summary>
    /// <param name="markdown">Markdown content.</param>
    /// <returns>Sanitized HTML output.</returns>
    string ToHtml(string? markdown);
}

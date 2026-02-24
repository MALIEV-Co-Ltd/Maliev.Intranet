using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>Tests for the MarkdownRenderer component.</summary>
public class MarkdownRendererTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<IMarkdownService> _markdownServiceMock;

    /// <summary>Initializes a new instance of the <see cref="MarkdownRendererTests"/> class.</summary>
    public MarkdownRendererTests()
    {
        _markdownServiceMock = new Mock<IMarkdownService>();
        Services.AddSingleton(_markdownServiceMock.Object);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Initializes the test asynchronously.</summary>
    public Task InitializeAsync() => Task.CompletedTask;
    /// <summary>Disposes resources used by the test asynchronously.</summary>
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    /// <summary>Verifies that HTML is rendered from the provided markdown input.</summary>
    public void ShouldRenderHtmlFromMarkdown()
    {
        var markdown = "# Hello";
        var html = "<h1>Hello</h1>";
        _markdownServiceMock.Setup(x => x.ToHtml(markdown)).Returns(html);

        var cut = Render<MarkdownRenderer>(parameters => parameters
            .Add(p => p.Markdown, markdown)
        );

        Assert.Contains(html, cut.Markup);
    }
}

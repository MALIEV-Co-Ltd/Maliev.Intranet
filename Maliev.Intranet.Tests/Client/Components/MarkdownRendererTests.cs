using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Maliev.Intranet.Tests.Client.Components;

public class MarkdownRendererTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<IMarkdownService> _markdownServiceMock;

    public MarkdownRendererTests()
    {
        _markdownServiceMock = new Mock<IMarkdownService>();
        Services.AddSingleton(_markdownServiceMock.Object);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
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

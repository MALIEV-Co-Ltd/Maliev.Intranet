using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Shared;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Tests.Client.Components;

public class DocumentListDisplayTests : BunitContext, IAsyncLifetime
{
    public DocumentListDisplayTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ShouldRenderEmptyMessage_WhenNoDocuments()
    {
        var cut = Render<DocumentListDisplay>(parameters => parameters
            .Add(p => p.Documents, new List<CreateDocumentRequest>())
        );

        Assert.Contains("No documents uploaded yet", cut.Markup);
    }

    [Fact]
    public void ShouldRenderDocuments()
    {
        var docs = new List<CreateDocumentRequest>
        {
            new() { FileName = "test.pdf", FileSize = 1024, DocumentCategory = "General" }
        };

        var cut = Render<DocumentListDisplay>(parameters => parameters
            .Add(p => p.Documents, docs)
        );

        Assert.Contains("test.pdf", cut.Markup);
    }
}

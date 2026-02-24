using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Shared;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>Tests for the DocumentListDisplay component.</summary>
public class DocumentListDisplayTests : BunitContext, IAsyncLifetime
{
    /// <summary>Initializes a new instance of the <see cref="DocumentListDisplayTests"/> class.</summary>
    public DocumentListDisplayTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Initializes the test asynchronously.</summary>
    public Task InitializeAsync() => Task.CompletedTask;
    /// <summary>Disposes resources used by the test asynchronously.</summary>
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    /// <summary>Verifies that an empty state message is rendered when no documents are present.</summary>
    public void ShouldRenderEmptyMessage_WhenNoDocuments()
    {
        var cut = Render<DocumentListDisplay>(parameters => parameters
            .Add(p => p.Documents, new List<CreateDocumentRequest>())
        );

        Assert.Contains("No documents uploaded yet", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that the list of documents is rendered correctly.</summary>
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

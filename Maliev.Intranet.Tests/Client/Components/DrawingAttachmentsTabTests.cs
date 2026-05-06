using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class DrawingAttachmentsTabTests : BunitContext, IAsyncLifetime
{
    private readonly MockHttpMessageHandler _httpHandler = new();

    public DrawingAttachmentsTabTests()
    {
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddSingleton(new HttpClient(_httpHandler) { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton(new UploadSettings());
        Services.AddSingleton(CreateFileTypesSettings());
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void Markup_KeepsFileUploadEnabledWhileUploading()
    {
        var source = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "Maliev.Intranet.Client",
            "Components",
            "Project",
            "DrawingAttachmentsTab.razor"));

        var uploadStart = source.IndexOf("<MudFileUpload", StringComparison.Ordinal);
        var uploadEnd = source.IndexOf("<CustomContent>", uploadStart, StringComparison.Ordinal);
        Assert.True(uploadStart >= 0);
        Assert.True(uploadEnd > uploadStart);

        var uploadMarkup = source[uploadStart..uploadEnd];
        Assert.DoesNotContain("Disabled", uploadMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleFilesSelected_WhenPartChangesDuringBatch_KeepsAllDrawingsOnOriginalPart()
    {
        var firstPart = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "first.stp",
        };
        var secondPart = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "second.stp",
        };
        var changedParts = new List<PartViewModel>();
        var requestedPartIds = new List<string>();
        var firstRequestStarted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseUploads = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCount = 0;

        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/v1/uploads/attachments")
            {
                requestedPartIds.Add(GetQueryValue(request.RequestUri, "partId"));
                firstRequestStarted.TrySetResult(null);
                await releaseUploads.Task.WaitAsync(ct);

                var attachmentIndex = Interlocked.Increment(ref requestCount);
                var attachments = new List<DraftProjectAttachmentDto>
                {
                    new()
                    {
                        FileId = Guid.NewGuid(),
                        Name = $"drawing-{attachmentIndex}.pdf",
                        StoragePath = $"projects/drawings/drawing-{attachmentIndex}.pdf",
                        FileType = "application/pdf",
                        FileSizeBytes = 1024,
                        Kind = DraftAttachmentKind.Drawing,
                        UploadedAt = DateTime.UtcNow,
                    },
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(attachments), Encoding.UTF8, "application/json"),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        var cut = Render<DrawingAttachmentsTab>(parameters => parameters
            .Add(p => p.Part, firstPart)
            .Add(p => p.TempProjectId, Guid.NewGuid())
            .Add(p => p.OnPartChanged, EventCallback.Factory.Create<PartViewModel>(
                this,
                part => changedParts.Add(part))));

        var uploadTask = InvokeHandleFilesSelectedAsync(cut,
        [
            new TestBrowserFile("drawing-a.pdf"),
            new TestBrowserFile("drawing-b.pdf"),
        ]);

        await firstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        typeof(DrawingAttachmentsTab)
            .GetProperty(nameof(DrawingAttachmentsTab.Part))!
            .SetValue(cut.Instance, secondPart);

        releaseUploads.SetResult(null);
        await uploadTask;

        Assert.Equal(2, firstPart.DrawingFiles.Count);
        Assert.Empty(secondPart.DrawingFiles);
        Assert.Equal(2, changedParts.Count);
        Assert.All(changedParts, part => Assert.Same(firstPart, part));
        Assert.All(requestedPartIds, partId => Assert.Equal(firstPart.FileId.ToString(), partId));
    }

    private static async Task InvokeHandleFilesSelectedAsync(
        RenderedComponent<DrawingAttachmentsTab> cut,
        IReadOnlyList<IBrowserFile> files)
    {
        var method = typeof(DrawingAttachmentsTab).GetMethod(
            "HandleFilesSelected",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        await cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, [files])!);
    }

    private static string GetQueryValue(Uri uri, string key)
    {
        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in query)
        {
            var parts = item.Split('=', 2);
            if (parts.Length == 2 && string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(parts[1]);
        }

        return string.Empty;
    }

    private static FileTypesSettings CreateFileTypesSettings() => new()
    {
        ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".stl", ".step", ".3mf", ".obj" },
        DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".dxf", ".dwg" },
        ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" },
        OfficeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".doc", ".docx", ".xls", ".xlsx" },
        ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".zip", ".rar", ".7z" },
        DrawingExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".dxf", ".dwg", ".png", ".jpg" },
        SupplementaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".doc", ".docx", ".xls", ".xlsx", ".zip" },
    };

    private static string GetRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "Maliev.Intranet.slnx")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Could not locate Maliev.Intranet repository root.");
    }

    private sealed class TestBrowserFile(string name) : IBrowserFile
    {
        public string Name { get; } = name;

        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

        public long Size { get; } = 1024;

        public string ContentType { get; } = "application/pdf";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
            new MemoryStream(Encoding.UTF8.GetBytes("drawing"));
    }
}

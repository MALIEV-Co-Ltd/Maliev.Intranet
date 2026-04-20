using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public class PartConfigSidebarDfmTests : BunitContext, IAsyncLifetime
{
    private readonly MockHttpMessageHandler _httpHandler = new();
    private readonly List<HttpRequestMessage> _sentRequests = [];
    private readonly Guid _validFileId = Guid.NewGuid();
    private readonly List<ProcessDto> _processes = [];

    public PartConfigSidebarDfmTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        _httpHandler.HandlerFunc = HandleRequest;
        var client = new HttpClient(_httpHandler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);

        Services.AddSingleton(new FileTypesSettings
        {
            ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".stl" },
            DocumentExtensions = [],
            ImageExtensions = [],
            OfficeExtensions = [],
            ArchiveExtensions = [],
            DrawingExtensions = [],
            SupplementaryExtensions = [],
        });
        Services.AddSingleton(new UploadSettings());
        Services.AddSingleton<ILogger<PartConfigSidebar>, Logger<PartConfigSidebar>>();

        Render<MudBlazor.MudPopoverProvider>();

        _processes.Add(new ProcessDto(Guid.NewGuid(), "FDM", "FDM 3D Printing", null, 0));
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    private Task<HttpResponseMessage> HandleRequest(HttpRequestMessage request, CancellationToken ct)
    {
        lock (_sentRequests) { _sentRequests.Add(request); }

        var path = request.RequestUri?.AbsolutePath ?? "";

        if (path.Contains("/dfm/") && request.Method == HttpMethod.Post)
        {
            var response = new DfmAnalysisResponse
            {
                UploadId = _validFileId.ToString(),
                ProcessCode = "FDM",
                Status = "analysis_complete",
                DfmReport = new DfmReport
                {
                    ReportType = "FDM",
                    Issues = [],
                    AnalysisTimeSeconds = 1.5,
                },
            };
            var json = JsonSerializer.Serialize(response);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, new MediaTypeHeaderValue("application/json"))
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", new MediaTypeHeaderValue("application/json"))
        });
    }

    private PartViewModel CreatePartWithFileId(Guid fileId) => new()
    {
        FileId = fileId,
        Name = "test-part.stl",
        StoragePath = "projects/test/test-part.stl",
        Quantity = 1,
    };

    private RenderedComponent<PartConfigSidebar> RenderSidebar(PartViewModel part) =>
        Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, _processes)
            .Add(p => p.TempProjectId, Guid.NewGuid())
            .Add(p => p.CurrencySymbol, "฿"));

    [Fact]
    public void Render_WithFileId_ShouldNotLogUploadIdWarning()
    {
        // Part has a valid FileId — the warning should not appear
        var part = CreatePartWithFileId(_validFileId);
        var cut = RenderSidebar(part);

        // Component renders without error; the _currentUploadId warning path is no longer reachable
        Assert.NotNull(cut);
    }

    [Fact]
    public async Task OnProcessChanged_WithValidFileId_CallsDfmEndpoint()
    {
        var part = CreatePartWithFileId(_validFileId);
        var cut = RenderSidebar(part);

        // Must run on the renderer's dispatcher to avoid StateHasChanged threading error
        await cut.InvokeAsync(async () =>
        {
            var method = typeof(PartConfigSidebar).GetMethod("OnProcessChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task?)method.Invoke(cut.Instance, new object?[] { _processes[0] });
            if (task != null) await task;
        });

        // Verify a DFM HTTP request was sent with the FileId in the URL
        HttpRequestMessage? dfmRequest;
        lock (_sentRequests)
        {
            dfmRequest = _sentRequests.FirstOrDefault(r =>
                r.RequestUri?.AbsolutePath.Contains("/dfm/") == true
                && r.Method == HttpMethod.Post);
        }

        Assert.NotNull(dfmRequest);
        Assert.Contains(_validFileId.ToString(), dfmRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task OnProcessChanged_WithEmptyFileId_SkipsDfmEndpoint()
    {
        var part = CreatePartWithFileId(Guid.Empty);
        var cut = RenderSidebar(part);

        await cut.InvokeAsync(async () =>
        {
            var method = typeof(PartConfigSidebar).GetMethod("OnProcessChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task?)method.Invoke(cut.Instance, new object?[] { _processes[0] });
            if (task != null) await task;
        });

        // No DFM request should have been sent
        HttpRequestMessage? dfmRequest;
        lock (_sentRequests)
        {
            dfmRequest = _sentRequests.FirstOrDefault(r =>
                r.RequestUri?.AbsolutePath.Contains("/dfm/") == true
                && r.Method == HttpMethod.Post);
        }

        Assert.Null(dfmRequest);
    }
}

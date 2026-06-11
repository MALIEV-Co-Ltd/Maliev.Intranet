using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Maliev.MessagingContracts.Contracts.Geometry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Maliev.Intranet.Tests.Client.Pages;

public class ProjectNewAutoSaveTests : BunitContext, IAsyncLifetime
{
    private readonly MockHttpMessageHandler _httpHandler = new();
    private readonly List<HttpRequestMessage> _sentRequests = [];

    public ProjectNewAutoSaveTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddAuthorizationCore();
        Services.AddCascadingAuthenticationState();

        Services.AddScoped<AuthenticationStateProvider, TestAuthenticationStateProvider>();
        var authServiceMock = new Mock<IAuthorizationService>();
        authServiceMock.Setup(x => x.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(),
            It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        authServiceMock.Setup(x => x.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());
        Services.AddSingleton(authServiceMock.Object);
        var layoutLoggerMock = new Mock<ILogger<LayoutService>>();
        Services.AddSingleton<LayoutService>(new LayoutService(JSInterop.JSRuntime, layoutLoggerMock.Object, null!));
        Services.AddSingleton<CookieProvider>();
        Services.AddSingleton<ChatService>();
        Services.AddScoped<BreadcrumbService>();
        Services.AddSingleton(new FileTypesSettings
        {
            ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".stl",
                ".step",
                ".stp",
                ".3mf",
                ".obj",
                ".igs",
                ".iges",
                ".fbx",
                ".glb",
                ".gltf",
            },
            DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".dxf", ".dwg" },
            ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" },
            OfficeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".doc", ".docx", ".xls", ".xlsx" },
            ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".zip", ".rar", ".7z" },
            DrawingExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".dxf", ".dwg", ".png", ".jpg" },
            SupplementaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".doc", ".docx", ".xls", ".xlsx", ".zip" }
        });
        var draftServiceMock = new Mock<IProjectDraftService>();
        draftServiceMock.Setup(s => s.LoadDraftAsync(It.IsAny<string?>())).ReturnsAsync((DraftProjectState?)null);
        draftServiceMock.Setup(s => s.SaveDraftAsync(It.IsAny<DraftProjectState>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        draftServiceMock.Setup(s => s.ClearDraftAsync(It.IsAny<string?>())).Returns(Task.CompletedTask);
        Services.AddSingleton(draftServiceMock.Object);
        Services.AddSingleton(new UploadSettings());
        Services.AddLogging();
        _httpHandler.HandlerFunc = DefaultHandler;
        var client = new HttpClient(_httpHandler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddScoped<CurrencyService>();
        Services.AddScoped<ShippingService>();
        Services.AddScoped<AlertService>();
        Services.AddSingleton<ThumbnailGenerationService>();
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void TestFileTypeSettings_CoverEveryProjectNewSupported3dExtension()
    {
        var fileTypes = Services.GetRequiredService<FileTypesSettings>();

        foreach (var extension in new[] { ".stl", ".step", ".stp", ".3mf", ".obj", ".igs", ".iges", ".fbx", ".glb", ".gltf" })
        {
            Assert.Contains(extension, fileTypes.ThreeDExtensions);
        }
    }

    private Task<HttpResponseMessage> DefaultHandler(HttpRequestMessage request, CancellationToken ct)
    {
        lock (_sentRequests) { _sentRequests.Add(request); }
        var path = request.RequestUri?.AbsolutePath ?? "";
        if (path.Contains("currencies"))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "[{\"Id\":\"" + Guid.NewGuid() + "\",\"Code\":\"THB\",\"Name\":\"Thai Baht\",\"Symbol\":\"฿\",\"DecimalPlaces\":2,\"IsActive\":true,\"IsPrimary\":true}]",
                    Encoding.UTF8, "application/json")
            });
        }
        if (path.Contains("processes") && !path.Contains("processes/"))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
        }
        if (path.Contains("lead-times"))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
        }
        if (path.Contains("projects") && request.Method == HttpMethod.Get)
        {
            var json = JsonSerializer.Serialize(new PagedResponse<ProjectSummaryDto>());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });
    }

    private void ClearRequests()
    {
        lock (_sentRequests) { _sentRequests.Clear(); }
    }

    [Fact]
    public void ProjectNew_ShouldRender_WithoutException()
    {
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        Assert.NotNull(cut.Instance);
    }

    [Fact]
    public void ProjectNew_ShouldRequestCurrenciesDuringInit()
    {
        ClearRequests();
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        Assert.Contains(_sentRequests, r =>
            r.RequestUri?.AbsolutePath.Contains("currencies") == true);
    }

    [Fact]
    public void ProjectNew_ShouldRequestProcessesDuringInit()
    {
        ClearRequests();
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        Assert.Contains(_sentRequests, r =>
            r.RequestUri?.AbsolutePath.Contains("processes") == true);
    }

    [Fact]
    public void ProjectNew_ShouldRequestLeadTimesDuringInit()
    {
        ClearRequests();
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        Assert.Contains(_sentRequests, r =>
            r.RequestUri?.AbsolutePath.Contains("lead-times") == true);
    }

    [Fact]
    public void ProjectNew_WithCustomerIdQuery_PreloadsSelectedCustomer()
    {
        var customerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.Equals($"/api/v1/customers/{customerId}", StringComparison.Ordinal))
            {
                lock (_sentRequests) { _sentRequests.Add(request); }
                var customer = new CustomerDetailDto
                {
                    Id = customerId,
                    Name = "Sarah Chen",
                    Email = "sarah@example.com",
                    ProfileImageUrl = "https://lh3.googleusercontent.com/a/sarah",
                    CompanyName = "Maliev Test Company",
                    Status = "Active",
                    Segment = "Retail",
                    Tier = "Bronze",
                    CreatedAt = new DateTime(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc)
                };
                var json = JsonSerializer.Serialize(customer);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            return await DefaultHandler(request, ct);
        };

        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/sales/projects/new?session={Guid.NewGuid()}&customerId={customerId}");

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();

        cut.WaitForAssertion(() => Assert.Contains("Sarah Chen", cut.Markup), TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => Assert.Contains("https://lh3.googleusercontent.com/a/sarah", cut.Markup), TimeSpan.FromSeconds(5));
        Assert.Contains(_sentRequests, request =>
            request.RequestUri?.AbsolutePath.Equals($"/api/v1/customers/{customerId}", StringComparison.Ordinal) == true);
        Assert.Contains($"customerId={customerId}", navigation.Uri);
    }

    [Fact]
    public async Task OpenBabylonViewer_WhenViewerUrlAlreadyExists_DoesNotRefreshOrShowError()
    {
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "review.stp",
            GlbStoragePath = "projects/temp/review.stp_viewer.glb",
            ViewerUrl = "https://signed.example/review.glb",
        };
        var snackbar = Services.GetRequiredService<ISnackbar>();
        ClearRequests();

        await InvokePrivateTaskWithArgsAsync(cut, "OpenBabylonViewer", part);

        Assert.Equal("https://signed.example/review.glb", part.ViewerUrl);
        Assert.DoesNotContain(_sentRequests, request =>
            request.RequestUri?.AbsolutePath.Contains("viewer-url", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(snackbar.ShownSnackbars, item =>
            item.Message == "Failed to load 3D viewer URL.");
    }

    [Fact]
    public async Task OpenBabylonViewer_WhenViewerUrlEmpty_CallsViewerUrlEndpointWithStoragePath()
    {
        const string storagePath = "projects/temp/review.stp";
        const string glbStoragePath = "projects/temp/review.stp_viewer.glb";
        const string signedUrl = "https://signed.example/review.glb";

        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }
            if (request.RequestUri?.AbsolutePath.Contains("viewer-url") == true)
            {
                var query = request.RequestUri.Query;
                if (query.Contains(Uri.EscapeDataString(storagePath)))
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(new { url = signedUrl }),
                            Encoding.UTF8, "application/json")
                    };
                // Fail if GlbStoragePath (with _viewer.glb suffix) was passed instead
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            return await DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "review.stp",
            StoragePath = storagePath,
            GlbStoragePath = glbStoragePath,
        };
        ClearRequests();

        await InvokePrivateTaskWithArgsAsync(cut, "OpenBabylonViewer", part);

        Assert.Equal(signedUrl, part.ViewerUrl);
        Assert.Contains(_sentRequests, request =>
            request.RequestUri?.AbsolutePath.Contains("viewer-url", StringComparison.OrdinalIgnoreCase) == true &&
            request.RequestUri.Query.Contains(Uri.EscapeDataString(storagePath)));
        Assert.DoesNotContain(_sentRequests, request =>
            request.RequestUri?.Query.Contains(Uri.EscapeDataString(glbStoragePath)) == true);
    }

    [Fact]
    public async Task RequestFreshViewerUrlAsync_WhenSignedUrlExpired_CallsViewerUrlEndpointWithStoragePath()
    {
        const string storagePath = "projects/temp/review.stp";
        const string glbStoragePath = "projects/temp/review.stp_viewer.glb";
        const string freshSignedUrl = "https://signed.example/review-fresh.glb";

        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }
            if (request.RequestUri?.AbsolutePath.Contains("viewer-url") == true)
            {
                var query = request.RequestUri.Query;
                if (query.Contains(Uri.EscapeDataString(storagePath)))
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(new { url = freshSignedUrl }),
                            Encoding.UTF8, "application/json")
                    };
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            return await DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var parts = GetParts(cut.Instance);
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "review.stp",
            StoragePath = storagePath,
            GlbStoragePath = glbStoragePath,
            ViewerUrl = "https://signed.example/review-expired.glb",
        };
        parts.Add(part);
        ClearRequests();

        await InvokePrivateTaskWithArgsAsync(cut, "RequestFreshViewerUrlAsync", storagePath);

        Assert.Equal(freshSignedUrl, part.ViewerUrl);
        Assert.Equal(freshSignedUrl, part.GlbSignedUrl);
        Assert.Contains(_sentRequests, request =>
            request.RequestUri?.AbsolutePath.Contains("viewer-url", StringComparison.OrdinalIgnoreCase) == true &&
            request.RequestUri.Query.Contains(Uri.EscapeDataString(storagePath)));
        Assert.DoesNotContain(_sentRequests, request =>
            request.RequestUri?.Query.Contains(Uri.EscapeDataString(glbStoragePath)) == true);
    }

    [Fact]
    public async Task RequestFreshViewerUrlAsync_WhenCalledWithGlbStoragePath_UsesSourceStoragePath()
    {
        const string storagePath = "projects/temp/review.stp";
        const string glbStoragePath = "projects/temp/review.stp_viewer.glb";
        const string freshSignedUrl = "https://signed.example/review-fresh.glb";

        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }
            if (request.RequestUri?.AbsolutePath.Contains("viewer-url") == true)
            {
                var query = request.RequestUri.Query;
                if (query.Contains(Uri.EscapeDataString(storagePath)))
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(new { url = freshSignedUrl }),
                            Encoding.UTF8, "application/json")
                    };
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            return await DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var parts = GetParts(cut.Instance);
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "review.stp",
            StoragePath = storagePath,
            GlbStoragePath = glbStoragePath,
            ViewerUrl = "https://signed.example/review-expired.glb",
        };
        parts.Add(part);
        ClearRequests();

        await InvokePrivateTaskWithArgsAsync(cut, "RequestFreshViewerUrlAsync", glbStoragePath);

        Assert.Equal(freshSignedUrl, part.ViewerUrl);
        Assert.Equal(freshSignedUrl, part.GlbSignedUrl);
        Assert.Contains(_sentRequests, request =>
            request.RequestUri?.AbsolutePath.Contains("viewer-url", StringComparison.OrdinalIgnoreCase) == true &&
            request.RequestUri.Query.Contains(Uri.EscapeDataString(storagePath)));
        Assert.DoesNotContain(_sentRequests, request =>
            request.RequestUri?.Query.Contains(Uri.EscapeDataString(glbStoragePath)) == true);
    }

    [Fact]
    public async Task TryCompleteBrowserPrimaryViewerLocallyAsync_WhenObjectUrlExists_MarksPreviewReady()
    {
        const string storagePath = "projects/temp/local-bracket.stl";
        const string objectUrl = "blob:http://test/local-bracket";

        JSInterop
            .Setup<string?>("window.projectNewUploads.getObjectUrl", _ => true)
            .SetResult(objectUrl);

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var part = new PartViewModel
        {
            Name = "local-bracket.stl",
            StoragePath = storagePath,
            ClientUploadId = "client-local-bracket",
            AwaitingPreview = true,
            StatusText = "Processing geometry..."
        };

        var completedLocally = await InvokePrivateTaskWithResultAsync<bool>(
            cut,
            "TryCompleteBrowserPrimaryViewerLocallyAsync",
            part);

        Assert.True(completedLocally);
        Assert.False(part.AwaitingPreview);
        Assert.Equal("Ready", part.StatusText);
        Assert.Equal(objectUrl, part.ViewerUrl);
        Assert.Equal(storagePath, part.ViewerStoragePath);
        Assert.Equal(".stl", part.ViewerFileExtension);
    }

    [Fact]
    public async Task HandleFileSelectedAsync_WhenTwoValidFilesSelected_StartsBothUploads()
    {
        var releaseUploads = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initiateCount = 0;

        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path == "/api/v1/uploads/resumable" && request.Method == HttpMethod.Post)
            {
                lock (_sentRequests) { _sentRequests.Add(request); }
                Interlocked.Increment(ref initiateCount);
                await releaseUploads.Task.WaitAsync(ct);
                return CreateResumableSessionResponse();
            }

            return await DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        try
        {
            await InvokeHandleFileSelectedAsync(cut, [
                new TestBrowserFile("alpha.stl", 1024),
                new TestBrowserFile("bravo.step", 2048)
            ]);

            cut.WaitForAssertion(() =>
            {
                var parts = GetParts(cut.Instance);
                Assert.Equal(2, parts.Count);
                Assert.Equal(2, parts.Count(p => p.Uploading));
                Assert.DoesNotContain(parts, p => p.QueuedUpload);
                Assert.Equal(2, Volatile.Read(ref initiateCount));
            });
        }
        finally
        {
            releaseUploads.TrySetResult(null);
        }
    }

    [Fact]
    public async Task HandleFileSelectedAsync_WhenFiveFilesSelected_StartsThreeAndQueuesTwo()
    {
        var releaseUploads = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initiateCount = 0;

        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path == "/api/v1/uploads/resumable" && request.Method == HttpMethod.Post)
            {
                lock (_sentRequests) { _sentRequests.Add(request); }
                Interlocked.Increment(ref initiateCount);
                await releaseUploads.Task.WaitAsync(ct);
                return CreateResumableSessionResponse();
            }

            return await DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        try
        {
            await InvokeHandleFileSelectedAsync(cut, [
                new TestBrowserFile("one.stl", 1024),
                new TestBrowserFile("two.stl", 1024),
                new TestBrowserFile("three.stl", 1024),
                new TestBrowserFile("four.stl", 1024),
                new TestBrowserFile("five.stl", 1024)
            ]);

            cut.WaitForAssertion(() =>
            {
                var parts = GetParts(cut.Instance);
                Assert.Equal(5, parts.Count);
                Assert.Equal(3, parts.Count(p => p.Uploading));
                Assert.Equal(2, parts.Count(p => p.QueuedUpload));
                Assert.Equal(3, Volatile.Read(ref initiateCount));
            });
        }
        finally
        {
            releaseUploads.TrySetResult(null);
        }
    }

    [Fact]
    public void UploadProgressCallback_WhenInvoked_UpdatesOnlyBoundPart()
    {
        var partA = new PartViewModel { Name = "alpha.stl" };
        var partB = new PartViewModel { Name = "bravo.stl" };
        var callbackType = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetNestedType("UploadProgressCallback", BindingFlags.NonPublic);
        Assert.NotNull(callbackType);

        var callback = Activator.CreateInstance(callbackType, [partB, (Action)(() => { })]);
        Assert.NotNull(callback);

        callbackType.GetMethod("OnUploadProgress")!.Invoke(callback, [62]);

        Assert.Equal(0, partA.ProgressPercent);
        Assert.Equal(62, partB.ProgressPercent);
    }

    [Fact]
    public async Task HandleFileSelectedAsync_WhenOneUploadFails_ContinuesOtherUploads()
    {
        var releaseUploads = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initiateCount = 0;

        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path == "/api/v1/uploads/resumable" && request.Method == HttpMethod.Post)
            {
                lock (_sentRequests) { _sentRequests.Add(request); }
                Interlocked.Increment(ref initiateCount);

                var body = await request.Content!.ReadAsStringAsync(ct);
                using var json = JsonDocument.Parse(body);
                var fileName = json.RootElement.GetProperty("fileName").GetString();
                if (fileName == "bad.stl")
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                await releaseUploads.Task.WaitAsync(ct);
                return CreateResumableSessionResponse();
            }

            return await DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        try
        {
            await InvokeHandleFileSelectedAsync(cut, [
                new TestBrowserFile("bad.stl", 1024),
                new TestBrowserFile("good-a.stl", 1024),
                new TestBrowserFile("good-b.stl", 1024),
                new TestBrowserFile("good-c.stl", 1024)
            ]);

            cut.WaitForAssertion(() =>
            {
                var parts = GetParts(cut.Instance);
                Assert.Equal(4, parts.Count);
                Assert.NotNull(parts.Single(p => p.Name == "bad.stl").Error);
                Assert.Equal(3, parts.Count(p => p.Uploading));
                Assert.DoesNotContain(parts.Where(p => p.Name != "bad.stl"), p => p.QueuedUpload);
                Assert.Equal(4, Volatile.Read(ref initiateCount));
            });
        }
        finally
        {
            releaseUploads.TrySetResult(null);
        }
    }

    [Fact]
    public void LoadRecentProjectsAsync_WhenApiReturnsProjects_RendersWithoutError()
    {
        var customerId = Guid.NewGuid();
        var draftProject = new ProjectSummaryDto
        {
            Id = Guid.NewGuid(),
            Title = "Draft",
            Status = "Draft",
            CustomerId = customerId,
            CustomerName = "Test",
            ProjectNumber = "PRJ-001",
            PartsCount = 0,
            TotalPrice = 0m,
            CreatedAt = DateTime.UtcNow,
        };
        var configuringProject = new ProjectSummaryDto
        {
            Id = Guid.NewGuid(),
            Title = "Configuring",
            Status = "Configuring",
            CustomerId = customerId,
            CustomerName = "Test",
            ProjectNumber = "PRJ-002",
            PartsCount = 1,
            TotalPrice = 100m,
            CreatedAt = DateTime.UtcNow,
        };
        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("projects") && request.Method == HttpMethod.Get
 && (request.RequestUri?.Query.Contains("customerId") ?? false))
            {
                var paged = new PagedResponse<ProjectSummaryDto>
                {
                    Data = [draftProject, configuringProject],
                    Meta = new PaginationMeta { TotalCount = 2, CurrentPage = 1, PageSize = 10, TotalPages = 1 },
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(paged), Encoding.UTF8, "application/json")
                });
            }
            return DefaultHandler(request, ct);
        };
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        Assert.NotNull(cut.Instance);
        Assert.Contains(_sentRequests, r =>
            r.RequestUri?.AbsolutePath.Contains("currencies") == true);
        Assert.Contains(_sentRequests, r =>
            r.RequestUri?.AbsolutePath.Contains("processes") == true);
        Assert.Contains(_sentRequests, r =>
            r.RequestUri?.AbsolutePath.Contains("lead-times") == true);
    }

    [Fact]
    public void LoadRecentProjectsAsync_WhenApiFails_ComponentRendersWithoutError()
    {
        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("projects") && request.Method == HttpMethod.Get)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
            return DefaultHandler(request, ct);
        };
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        Assert.NotNull(cut.Instance);
    }

    [Fact]
    public void ResumeFromServerAsync_WhenResumeParamSet_FetchesProjectDetail()
    {
        var projectId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("projects") && !path.Contains("parts") && request.Method == HttpMethod.Get
                && request.RequestUri is not null && !request.RequestUri.Query.Contains("customerId")
                && path.Split('/').LastOrDefault() == projectId.ToString())
            {
                var detail = new ProjectDetailDto
                {
                    Id = projectId,
                    CustomerId = customerId,
                    CustomerName = "Test Customer",
                    Title = "Resumed Project",
                    Description = "A resumed draft",
                    Status = "Draft",
                    Currency = "THB",
                    Parts = [],
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(detail), Encoding.UTF8, "application/json")
                });
            }
            return DefaultHandler(request, ct);
        };
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        Assert.NotNull(cut.Instance);
    }

    [Fact]
    public void ResumeFromServerAsync_WhenProjectStatusIsNotDraft_SkipsResume()
    {
        var projectId = Guid.NewGuid();
        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("projects") && request.Method == HttpMethod.Get
                && path.Split('/').LastOrDefault() == projectId.ToString())
            {
                var detail = new ProjectDetailDto
                {
                    Id = projectId,
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Test Customer",
                    Title = "Submitted Project",
                    Status = "Submitted",
                    Currency = "THB",
                    Parts = [],
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(detail), Encoding.UTF8, "application/json")
                });
            }
            return DefaultHandler(request, ct);
        };
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        Assert.NotNull(cut.Instance);
    }

    [Fact]
    public void ResumeFromServerAsync_WhenProjectStatusIsQuotationGenerated_LoadsProjectForEditing()
    {
        var projectId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/sales/projects/new?session={sessionId}&resume={projectId}");

        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("projects") && request.Method == HttpMethod.Get
                && path.Split('/').LastOrDefault() == projectId.ToString())
            {
                var detail = new ProjectDetailDto
                {
                    Id = projectId,
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Axion Robotics",
                    Title = "Generated Quote Project",
                    Status = "QuotationGenerated",
                    Currency = "THB",
                    Parts =
                    [
                        new ProjectPartDto
                        {
                            Id = Guid.NewGuid(),
                            FileId = Guid.NewGuid(),
                            FileName = "quoted-part.stl",
                            ProcessType = "FDM",
                            MaterialName = "PLA",
                            Quantity = 2,
                            ConfirmedPrice = 125m
                        }
                    ],
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(detail), Encoding.UTF8, "application/json")
                });
            }
            return DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();

        cut.WaitForAssertion(() => Assert.Contains("Generated Quote Project", cut.Markup));
        Assert.Contains("quoted-part.stl", cut.Markup);
    }

    [Fact]
    public void CreatePartViewModelFromProjectPart_WhenResumedPartHasNoPersistedDfmReport_StopsDfmAnalyzingState()
    {
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var part = CreatePartViewModelFromProjectPart(cut, new ProjectPartDto
        {
            Id = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            FileName = "resumed-warning.stl",
            FileReference = "customers/customer-1/projects/project-1/resumed-warning.stl",
            ProcessType = "FDM",
            Quantity = 1,
            GlbStoragePath = "customers/customer-1/projects/project-1/resumed-warning_viewer.glb",
            ModelPreviewUrl = "https://storage.example/resumed-warning.glb",
            HasDfmWarnings = true,
        });

        Assert.Null(part.DfmReport);
        Assert.True(part.DfmAnalysisTimedOut);
        Assert.Equal("DFM_REPORT_UNAVAILABLE", part.AnalysisErrorCode);
        Assert.Equal("DFM report unavailable", part.StatusText);
    }

    [Fact]
    public void CreatePartViewModelFromProjectPart_WhenProjectServiceReturnsEnumProcess_RestoresCatalogConfiguration()
    {
        var processId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        SetPrivateField(cut.Instance, "_processes", new List<ProcessDto>
        {
            new(processId, "CNC_MILL", "CNC Milling", null, 10),
        });

        var part = CreatePartViewModelFromProjectPart(cut, new ProjectPartDto
        {
            Id = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            FileName = "restored-cnc.stl",
            FileReference = "customers/customer-1/projects/project-1/restored-cnc.stl",
            ProcessType = "CNC_Milling",
            MaterialId = materialId,
            MaterialName = "Brass C360",
            MaterialCode = "BRASS_C360",
            Quantity = 25,
            Finish = "MIRROR_POLISH",
            Tolerance = "ISO2768_M",
            RoughnessCode = "RA_1_6",
            DfmAcknowledged = true,
            HasDfmWarnings = true,
            HasThreadedHoles = true,
            ThreadedHoleSpec = "M4x0.7",
            ThreadedHoleCount = 6,
            HasInserts = true,
            InsertType = InsertType.HeatSet,
            InsertCount = 4,
            BagAndTag = false,
            InspectionLevel = InspectionLevel.Dimensional,
            Certificates = ["MaterialCert"],
            DrawingFiles =
            [
                new ProjectPartAttachmentDto
                {
                    FileId = Guid.NewGuid(),
                    FileName = "restored-cnc-drawing.pdf",
                    StoragePath = "customers/customer-1/projects/project-1/restored-cnc-drawing.pdf",
                    ContentType = "application/pdf",
                },
            ],
            ProcessConfig = new Dictionary<string, string>
            {
                ["cnc_setup"] = "three_axis",
            },
            Dimensions = new ModelDimensionsDto { X = 25, Y = 25, Z = 25 },
            IsManifold = true,
        });

        Assert.Equal("CNC_MILL", part.ProcessCode);
        Assert.Equal(processId, part.ProcessId);
        Assert.Equal(materialId, part.MaterialId);
        Assert.Equal("BRASS_C360", part.MaterialCode);
        Assert.Equal(25, part.Quantity);
        Assert.Equal("MIRROR_POLISH", part.FinishCode);
        Assert.Equal("ISO2768_M", part.ToleranceCode);
        Assert.Equal("RA_1_6", part.RoughnessCode);
        Assert.True(part.DfmAcknowledged);
        Assert.True(part.HasThreadedHoles);
        Assert.Equal("M4x0.7", part.ThreadedHoleSpec);
        Assert.Equal(6, part.ThreadedHoleCount);
        Assert.True(part.HasInserts);
        Assert.Equal(InsertType.HeatSet, part.InsertType);
        Assert.Equal(4, part.InsertCount);
        Assert.False(part.BagAndTag);
        Assert.Equal(InspectionLevel.Dimensional, part.InspectionLevel);
        Assert.Equal(["MaterialCert"], part.Certificates);
        Assert.Single(part.DrawingFiles);
        Assert.Equal("three_axis", part.ProcessOptionValues["cnc_setup"]);
        Assert.True(part.IsFullyConfigured);
    }

    [Fact]
    public async Task DuplicateProjectAsync_WhenServerDraftExists_PreservesCustomerConfigSelectionsAndFiles()
    {
        var sourceProjectId = Guid.NewGuid();
        var duplicatedProjectId = Guid.NewGuid();
        var sourcePartId = Guid.NewGuid();
        var duplicatedPartId = Guid.NewGuid();
        var sourceFileId = Guid.NewGuid();
        var duplicatedFileId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var processId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        string? duplicateBody = null;

        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path == $"/api/v1/projects/{sourceProjectId}" && request.Method == HttpMethod.Put)
                return new HttpResponseMessage(HttpStatusCode.NoContent);

            if (path == $"/api/v1/projects/{sourceProjectId}/parts/{sourcePartId}" && request.Method == HttpMethod.Put)
                return new HttpResponseMessage(HttpStatusCode.NoContent);

            if (path == $"/api/v1/projects/{sourceProjectId}/duplicate" && request.Method == HttpMethod.Post)
            {
                duplicateBody = await request.Content!.ReadAsStringAsync(ct);
                var duplicated = new ProjectDetailDto
                {
                    Id = duplicatedProjectId,
                    CustomerId = customerId,
                    CustomerName = "MaliEV Manufacturing",
                    Title = "Repeat bracket (Copy)",
                    Status = "Draft",
                    Currency = "THB",
                    Parts =
                    [
                        new ProjectPartDto
                        {
                            Id = duplicatedPartId,
                            FileId = duplicatedFileId,
                            FileReference = "customers/customer-1/projects/copied/bracket.stl",
                            FileName = "bracket.stl",
                            ProcessType = "CNC_MILL",
                            MaterialId = materialId,
                            MaterialCode = "AL6061",
                            Quantity = 6,
                            Finish = "BEAD_BLAST",
                            Tolerance = "ISO2768_M",
                            ThumbnailUrl = "https://signed.example/thumb.webp",
                            ThumbnailSmallGcsPath = "customers/customer-1/projects/copied/thumb-small.webp",
                            ThumbnailLargeGcsPath = "customers/customer-1/projects/copied/thumb-large.webp",
                            GlbStoragePath = "customers/customer-1/projects/copied/viewer.glb",
                            ModelPreviewUrl = "https://signed.example/viewer.glb",
                            OverlayPaths = new Dictionary<string, string>
                            {
                                ["CNC__sharp_corner"] = "customers/customer-1/projects/copied/overlays/sharp.glb"
                            },
                            RoughnessCode = "RA_1_6",
                            MarkingType = PartMarkingType.Laser,
                            MarkingText = "LOT-42",
                            DfmAcknowledged = true,
                            HasThreadedHoles = true,
                            ThreadedHoleSpec = "M4",
                            ThreadedHoleCount = 2,
                            HasInserts = true,
                            InsertType = InsertType.HeatSet,
                            InsertCount = 2,
                            BagAndTag = false,
                            InspectionLevel = InspectionLevel.Dimensional,
                            Certificates = ["MaterialCert"],
                            DrawingFiles =
                            [
                                new ProjectPartAttachmentDto
                                {
                                    FileId = Guid.NewGuid(),
                                    FileName = "bracket-drawing.pdf",
                                    StoragePath = "customers/customer-1/projects/copied/drawings/bracket-drawing.pdf",
                                    ContentType = "application/pdf",
                                    SizeBytes = 2048,
                                    UploadedAt = DateTime.UtcNow
                                }
                            ],
                            SupplementaryFiles =
                            [
                                new ProjectPartAttachmentDto
                                {
                                    FileId = Guid.NewGuid(),
                                    FileName = "readme.txt",
                                    StoragePath = "customers/customer-1/projects/copied/supplementary/readme.txt",
                                    ContentType = "text/plain",
                                    SizeBytes = 128,
                                    UploadedAt = DateTime.UtcNow
                                }
                            ],
                            ProcessConfig = new Dictionary<string, string>
                            {
                                ["deburring"] = "standard"
                            },
                            BodyCount = 1,
                            BodiesJson = "[{\"index\":0,\"name\":\"Body_01\",\"volumeCm3\":12.5,\"bboxMin\":[0,0,0],\"bboxMax\":[10,20,30],\"colorHex\":\"#4488CC\"}]",
                            SelectedBodyIndex = 0
                        }
                    ],
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(duplicated), Encoding.UTF8, "application/json")
                };
            }

            return await DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        SetPrivateField(cut.Instance, "_serverProjectId", (Guid?)sourceProjectId);
        SetPrivateField(cut.Instance, "_title", "Repeat bracket");
        SetPrivateField(cut.Instance, "_selectedCustomer", new CustomerSummaryDto
        {
            Id = customerId,
            Name = "MaliEV Manufacturing",
            Email = "orders@example.test",
        });
        SetPrivateField(cut.Instance, "_selectedLeadTime", new LeadTimeOptionDto("STANDARD", "Standard", 7, 10, 1m, true));
        SetPrivateField(cut.Instance, "_processes", new List<ProcessDto>
        {
            new(processId, "CNC_MILL", "CNC Milling", null, 10),
        });
        GetParts(cut.Instance).Add(new PartViewModel
        {
            FileId = sourceFileId,
            ServerPartId = sourcePartId,
            Name = "bracket.stl",
            StoragePath = "customers/customer-1/projects/source/bracket.stl",
            ProcessId = processId,
            ProcessCode = "CNC_MILL",
            MaterialId = materialId,
            MaterialCode = "AL6061",
            Quantity = 6,
            FinishCode = "BEAD_BLAST",
            ToleranceCode = "ISO2768_M",
            RoughnessCode = "RA_1_6",
            MarkingType = PartMarkingType.Laser,
            MarkingText = "LOT-42",
            DfmAcknowledged = true,
            HasThreadedHoles = true,
            ThreadedHoleSpec = "M4",
            ThreadedHoleCount = 2,
            HasInserts = true,
            InsertType = InsertType.HeatSet,
            InsertCount = 2,
            BagAndTag = false,
            InspectionLevel = InspectionLevel.Dimensional,
            Certificates = ["MaterialCert"],
            ProcessOptionValues = new Dictionary<string, string?> { ["deburring"] = "standard" },
            DrawingFiles =
            [
                new DraftProjectAttachmentDto
                {
                    FileId = Guid.NewGuid(),
                    Name = "bracket-drawing.pdf",
                    StoragePath = "customers/customer-1/projects/source/drawings/bracket-drawing.pdf",
                    FileType = "application/pdf",
                    FileSizeBytes = 2048,
                    Kind = DraftAttachmentKind.Drawing
                }
            ],
        });

        await InvokePrivateTaskAsync(cut, "DuplicateProjectAsync");

        Assert.NotNull(duplicateBody);
        using (var requestJson = JsonDocument.Parse(duplicateBody))
        {
            Assert.Equal("Repeat bracket (Copy)", requestJson.RootElement.GetProperty("title").GetString());
        }

        Assert.Equal(duplicatedProjectId, GetPrivateField<Guid>(cut.Instance, "_serverProjectId"));
        var selectedCustomer = GetPrivateField<CustomerSummaryDto?>(cut.Instance, "_selectedCustomer");
        Assert.Equal(customerId, selectedCustomer?.Id);

        var duplicatedPart = Assert.Single(GetParts(cut.Instance));
        Assert.Equal(duplicatedPartId, duplicatedPart.ServerPartId);
        Assert.Equal(duplicatedFileId, duplicatedPart.FileId);
        Assert.Equal("customers/customer-1/projects/copied/bracket.stl", duplicatedPart.StoragePath);
        Assert.Equal("CNC_MILL", duplicatedPart.ProcessCode);
        Assert.Equal(processId, duplicatedPart.ProcessId);
        Assert.Equal(materialId, duplicatedPart.MaterialId);
        Assert.Equal("RA_1_6", duplicatedPart.RoughnessCode);
        Assert.Equal(PartMarkingType.Laser, duplicatedPart.MarkingType);
        Assert.True(duplicatedPart.DfmAcknowledged);
        Assert.True(duplicatedPart.HasThreadedHoles);
        Assert.True(duplicatedPart.HasInserts);
        Assert.False(duplicatedPart.BagAndTag);
        Assert.Equal(InspectionLevel.Dimensional, duplicatedPart.InspectionLevel);
        Assert.Equal("MaterialCert", Assert.Single(duplicatedPart.Certificates));
        Assert.Equal("bracket-drawing.pdf", Assert.Single(duplicatedPart.DrawingFiles).Name);
        Assert.Equal("readme.txt", Assert.Single(duplicatedPart.SupplementaryFiles).Name);
        Assert.Equal("deburring", Assert.Single(duplicatedPart.ProcessOptionValues).Key);
        Assert.Equal("customers/customer-1/projects/copied/thumb-small.webp", duplicatedPart.ThumbnailSmallGcsPath);
        Assert.Equal("customers/customer-1/projects/copied/viewer.glb", duplicatedPart.GlbStoragePath);
        Assert.Equal("customers/customer-1/projects/copied/overlays/sharp.glb", duplicatedPart.OverlayPaths?["CNC__sharp_corner"]);
        Assert.Equal(1, duplicatedPart.BodyCount);
        Assert.Equal(0, duplicatedPart.SelectedBodyIndex);
        Assert.Single(duplicatedPart.Bodies);

        var snackbar = Services.GetRequiredService<ISnackbar>();
        Assert.Contains(snackbar.ShownSnackbars, item => item.Message == "Project duplicated.");
        Assert.DoesNotContain(snackbar.ShownSnackbars, item =>
            item.Message?.Contains("re-upload", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task ApplyProjectDetailAsync_WhenQuotedProjectRestored_RestoresCommercialTermsAndPartNotes()
    {
        var projectId = Guid.NewGuid();
        var quotationId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        const string customerProfileImageUrl = "https://lh3.googleusercontent.com/a/maliev-manufacturing";

        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path == $"/api/v1/customers/{customerId}" && request.Method == HttpMethod.Get)
            {
                var customer = new CustomerDetailDto
                {
                    Id = customerId,
                    Name = "MaliEV Manufacturing",
                    Email = "orders@maliev.com",
                    ProfileImageUrl = customerProfileImageUrl
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(customer), Encoding.UTF8, "application/json")
                };
            }

            if (path == $"/api/v1/quotations/{quotationId}" && request.Method == HttpMethod.Get)
            {
                var quotation = new QuotationDetailDto
                {
                    Id = quotationId,
                    QuotationNumber = "QT-RESTORE-001",
                    CustomerId = customerId,
                    CustomerName = "MaliEV Manufacturing",
                    CurrentVersionNumber = 2,
                    CurrencyCode = "THB",
                    Versions =
                    [
                        new QuotationVersionDto
                        {
                            VersionNumber = 1,
                            ManualDiscountAmount = 10m,
                            ShippingCost = 20m,
                            SpecialTerms = "Old terms"
                        },
                        new QuotationVersionDto
                        {
                            VersionNumber = 2,
                            ManualDiscountAmount = 125m,
                            ShippingCost = 450m,
                            SpecialTerms = "50% deposit before production."
                        }
                    ]
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(quotation), Encoding.UTF8, "application/json")
                };
            }

            return await DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var project = new ProjectDetailDto
        {
            Id = projectId,
            CustomerId = customerId,
            CustomerName = "MaliEV Manufacturing",
            Title = "Repeat bracket",
            Status = "QuotationGenerated",
            Currency = "THB",
            QuotationId = quotationId,
            Parts =
            [
                new ProjectPartDto
                {
                    Id = partId,
                    FileId = fileId,
                    FileReference = "customers/customer-1/projects/repeat/bracket.stl",
                    FileName = "bracket.stl",
                    ProcessType = "CNC_MILL",
                    Quantity = 4,
                    PartNotes = "Deburr all edges before anodizing."
                }
            ]
        };

        await InvokePrivateTaskWithArgsAsync(cut, "ApplyProjectDetailAsync", project);

        Assert.Equal(450m, GetPrivateField<decimal>(cut.Instance, "_shippingCost"));
        Assert.Equal(125m, GetPrivateField<decimal>(cut.Instance, "_manualDiscountAmount"));
        Assert.Equal("50% deposit before production.", GetPrivateField<string?>(cut.Instance, "_quotationTerms"));
        Assert.Equal("Deburr all edges before anodizing.", Assert.Single(GetParts(cut.Instance)).PartNotes);
        var selectedCustomer = GetPrivateField<CustomerSummaryDto?>(cut.Instance, "_selectedCustomer");
        Assert.NotNull(selectedCustomer);
        Assert.Equal(customerProfileImageUrl, selectedCustomer.ProfileImageUrl);
    }

    [Fact]
    public void BuildUpdateProjectPartRequest_AfterBulkPatch_UsesEditedCoreConfiguration()
    {
        var page = new global::Maliev.Intranet.Client.Pages.ProjectNew();
        var material = new CatalogMaterialDto(Guid.NewGuid(), "Aluminum 6061", "AL6061", "Metal", null, null, 10);
        var finish = new CatalogSurfaceFinishDto(Guid.NewGuid(), "Bead blast", "BEAD_BLAST", 1.6m, 8m, null, 20);
        var tolerance = new CatalogToleranceDto(Guid.NewGuid(), "ISO 2768 Fine", "ISO2768_F", "ISO 2768", "f", null, 12m, 20);
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "bulk-edited.stl",
            ProcessCode = "CNC_MILL",
            ProcessId = Guid.NewGuid(),
            AvailableMaterials = [material],
            AvailableFinishes = [finish],
            AvailableTolerances = [tolerance],
            Quantity = 1,
            InspectionLevel = InspectionLevel.Standard,
        };

        ProjectPartBulkEdit.ApplyPatch(part, new PartConfigurationBulkPatch
        {
            IncludeMaterial = true,
            Material = material,
            IncludeFinish = true,
            Finish = finish,
            IncludeTolerance = true,
            Tolerance = tolerance,
            IncludeQuantity = true,
            Quantity = 12,
            IncludeInspection = true,
            InspectionLevel = InspectionLevel.Dimensional,
        });

        var request = BuildUpdateProjectPartRequest(page, part);

        Assert.NotNull(request);
        Assert.Equal("CNC_MILL", request.ProcessType);
        Assert.Equal(material.Id, request.MaterialId);
        Assert.Equal(material.Code, request.MaterialCode);
        Assert.Equal(finish.Code, request.Finish);
        Assert.Equal(tolerance.Code, request.Tolerance);
        Assert.Equal(12, request.Quantity);
        Assert.Equal(InspectionLevel.Dimensional, request.InspectionLevel);
    }

    [Fact]
    public void ResumeFromServerAsync_WhenApiReturns404_ComponentRendersWithoutError()
    {
        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("projects") && request.Method == HttpMethod.Get
                && !request.RequestUri?.Query.Contains("customerId") == true
                && path.Split('/').Length > 2 && Guid.TryParse(path.Split('/').Last(), out _))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
            return DefaultHandler(request, ct);
        };
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        Assert.NotNull(cut.Instance);
    }

    [Fact]
    public async Task MigrateTempProjectFilesAsync_WhenAnyTempPartIsStillProcessing_DoesNotCallMigrationEndpoint()
    {
        var customerId = Guid.NewGuid();

        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }

            if (request.RequestUri?.AbsolutePath == "/api/v1/uploads/migrate-project")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            return DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        SetPrivateField(cut.Instance, "_selectedCustomer", new CustomerSummaryDto
        {
            Id = customerId,
            Name = "MaliEV Manufacturing",
            Email = "orders@example.test",
        });
        GetParts(cut.Instance).Add(new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "ready.stl",
            StoragePath = "projects/temp-project/ready.stl",
            GlbStoragePath = "projects/temp-project/ready.stl_viewer.glb",
            AwaitingPreview = false,
            StatusText = "Ready"
        });
        GetParts(cut.Instance).Add(new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "processing.stl",
            StoragePath = "projects/temp-project/processing.stl",
            AwaitingPreview = true,
            StatusText = "Processing geometry..."
        });

        await InvokePrivateTaskAsync(cut, "MigrateTempProjectFilesAsync");

        Assert.DoesNotContain(_sentRequests, r => r.RequestUri?.AbsolutePath == "/api/v1/uploads/migrate-project");
    }

    [Fact]
    public async Task MigrateTempProjectFilesAsync_WhenServerProjectIdDiffersFromStorageProjectId_UsesStorageProjectId()
    {
        var customerId = Guid.NewGuid();
        var serverProjectId = Guid.NewGuid();
        var storageProjectId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var requestedProjectId = Guid.Empty;
        var oldBasePath = $"projects/{storageProjectId}/ready.stl";
        var newBasePath = $"customers/customer-1/projects/{storageProjectId}/ready.stl";

        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }

            if (request.RequestUri?.AbsolutePath == "/api/v1/uploads/migrate-project")
            {
                var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
                requestedProjectId = Guid.Parse(query["projectId"]!);
                var migrationResponse = new BffMigrateProjectResponseDto
                {
                    DryRun = false,
                    TotalEvaluated = 1,
                    TotalMigrated = 1,
                    MigratedFiles =
                    [
                        new BffMigratedProjectFileDto
                        {
                            FileId = fileId.ToString(),
                            OldPath = oldBasePath,
                            NewPath = newBasePath
                        }
                    ],
                    Errors = []
                };

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(migrationResponse), Encoding.UTF8, "application/json")
                });
            }

            return DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        SetPrivateField(cut.Instance, "_serverProjectId", (Guid?)serverProjectId);
        SetPrivateField(cut.Instance, "_tempProjectId", serverProjectId);
        SetPrivateField(cut.Instance, "_selectedCustomer", new CustomerSummaryDto
        {
            Id = customerId,
            Name = "MaliEV Manufacturing",
            Email = "orders@example.test",
        });
        GetParts(cut.Instance).Add(new PartViewModel
        {
            FileId = fileId,
            Name = "ready.stl",
            StoragePath = oldBasePath,
            GlbStoragePath = oldBasePath + "_viewer.glb",
            AwaitingPreview = false,
            StatusText = "Ready"
        });

        await InvokePrivateTaskAsync(cut, "MigrateTempProjectFilesAsync");

        Assert.Equal(storageProjectId, requestedProjectId);
        Assert.Equal(newBasePath, GetParts(cut.Instance).Single().StoragePath);
    }

    [Fact]
    public async Task MigrateTempProjectFilesAsync_WhenPartHasGeneratedArtifacts_RewritesViewerPathsAndClearsStaleSignedUrls()
    {
        var customerId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        const string OldBasePath = "projects/temp-project/bracket.stl";
        const string NewBasePath = "customers/customer-1/projects/temp-project/bracket.stl";

        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }

            if (request.RequestUri?.AbsolutePath == "/api/v1/uploads/migrate-project")
            {
                var migrationResponse = new BffMigrateProjectResponseDto
                {
                    DryRun = false,
                    TotalEvaluated = 1,
                    TotalMigrated = 1,
                    MigratedFiles =
                    [
                        new BffMigratedProjectFileDto
                        {
                            FileId = fileId.ToString(),
                            OldPath = OldBasePath,
                            NewPath = NewBasePath
                        }
                    ],
                    Errors = []
                };

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(migrationResponse), Encoding.UTF8, "application/json")
                });
            }

            return DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        SetPrivateField(cut.Instance, "_selectedCustomer", new CustomerSummaryDto
        {
            Id = customerId,
            Name = "MaliEV Manufacturing",
            Email = "orders@example.test",
        });
        var part = new PartViewModel
        {
            FileId = fileId,
            Name = "bracket.stl",
            StoragePath = OldBasePath,
            ThumbnailSmallGcsPath = OldBasePath + "_thumb_256.webp",
            ThumbnailLargeGcsPath = OldBasePath + "_thumb_1200.webp",
            GlbStoragePath = OldBasePath + "_viewer.glb",
            GlbSignedUrl = "https://signed.example/old-viewer.glb",
            ViewerUrl = "https://signed.example/old-viewer.glb",
            AwaitingPreview = false,
            StatusText = "Ready",
            OverlayUrls = new Dictionary<string, string>
            {
                ["CNC__sharp_corner"] = "https://signed.example/old-overlay.glb"
            },
            OverlayPaths = new Dictionary<string, string>
            {
                ["CNC__sharp_corner"] = OldBasePath + "_overlays/sharp.glb"
            }
        };
        GetParts(cut.Instance).Add(part);

        await InvokePrivateTaskAsync(cut, "MigrateTempProjectFilesAsync");

        Assert.Equal(NewBasePath, part.StoragePath);
        Assert.Contains(OldBasePath, part.StoragePathAliases);
        Assert.Equal(NewBasePath + "_thumb_256.webp", part.ThumbnailSmallGcsPath);
        Assert.Equal(NewBasePath + "_thumb_1200.webp", part.ThumbnailLargeGcsPath);
        Assert.Equal(NewBasePath + "_viewer.glb", part.GlbStoragePath);
        Assert.Null(part.GlbSignedUrl);
        Assert.Null(part.ViewerUrl);
        Assert.Equal(NewBasePath + "_overlays/sharp.glb", part.OverlayPaths?["CNC__sharp_corner"]);
        Assert.Null(part.OverlayUrls);
    }

    [Fact]
    public async Task MigrateTempProjectFilesAsync_WhenMigrationReturnsCompletedStatus_AppliesStatusImmediately()
    {
        var customerId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        const string OldBasePath = "projects/temp-project/completed.stl";
        const string NewBasePath = "customers/customer-1/projects/temp-project/completed.stl";

        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }

            if (request.RequestUri?.AbsolutePath == "/api/v1/uploads/migrate-project")
            {
                var migrationResponse = new BffMigrateProjectResponseDto
                {
                    DryRun = false,
                    TotalEvaluated = 1,
                    TotalMigrated = 1,
                    MigratedFiles =
                    [
                        new BffMigratedProjectFileDto
                        {
                            FileId = fileId.ToString(),
                            OldPath = OldBasePath,
                            NewPath = NewBasePath,
                            Status = new FileAnalysisStatusDto
                            {
                                UploadId = NewBasePath,
                                Status = FileAnalysisStatus.Completed,
                                Dimensions = new FileAnalysisDimensionsDto
                                {
                                    X = 80,
                                    Y = 149,
                                    Z = 5,
                                    VolumeMm3 = 59600
                                },
                                IsManifold = true,
                                ThumbnailUrl = "https://signed.example/thumb-small.webp",
                                HiResThumbnailUrl = "https://signed.example/thumb-large.webp",
                                PreviewUrls = new FileAnalysisPreviewUrlsDto
                                {
                                    ThumbnailSmall = "https://signed.example/thumb-small.webp",
                                    ThumbnailLargeUrl = "https://signed.example/thumb-large.webp",
                                    ThumbnailSmallGcsPath = NewBasePath + "_thumb_256.webp",
                                    ThumbnailLargeGcsPath = NewBasePath + "_thumb_1200.webp"
                                },
                                GlbStoragePath = NewBasePath + "_viewer.glb",
                                GlbSignedUrl = "https://signed.example/viewer.glb",
                                PreviewProcessingStatus = PreviewProcessingStatus.Completed,
                            }
                        }
                    ],
                    Errors = []
                };

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(migrationResponse), Encoding.UTF8, "application/json")
                });
            }

            return DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        SetPrivateField(cut.Instance, "_selectedCustomer", new CustomerSummaryDto
        {
            Id = customerId,
            Name = "MaliEV Manufacturing",
            Email = "orders@example.test",
        });
        var part = new PartViewModel
        {
            FileId = fileId,
            Name = "completed.stl",
            StoragePath = OldBasePath,
            GlbStoragePath = OldBasePath + "_viewer.glb",
            AwaitingPreview = false,
            StatusText = "Ready"
        };
        GetParts(cut.Instance).Add(part);

        await InvokePrivateTaskAsync(cut, "MigrateTempProjectFilesAsync");

        Assert.Equal(NewBasePath, part.StoragePath);
        Assert.Equal("Ready", part.StatusText);
        Assert.False(part.AwaitingPreview);
        Assert.Equal(80, part.Dimensions?.X);
        Assert.Equal(149, part.Dimensions?.Y);
        Assert.Equal(5, part.Dimensions?.Z);
        Assert.Equal(59600, part.VolumeMm3);
        Assert.True(part.IsManifold);
        Assert.Equal("https://signed.example/thumb-small.webp", part.ThumbnailSmallUrl);
        Assert.Equal("https://signed.example/thumb-large.webp", part.ThumbnailLargeUrl);
        Assert.Equal(NewBasePath + "_thumb_256.webp", part.ThumbnailSmallGcsPath);
        Assert.Equal(NewBasePath + "_thumb_1200.webp", part.ThumbnailLargeGcsPath);
        Assert.Equal(NewBasePath + "_viewer.glb", part.GlbStoragePath);
        Assert.Equal("https://signed.example/viewer.glb", part.GlbSignedUrl);
        Assert.Equal("https://signed.example/viewer.glb", part.ViewerUrl);
    }

    [Fact]
    public async Task CreateProjectAndQuoteAsync_WhenPartsHavePrices_ConfirmsPartPricesBeforeGeneratingQuotation()
    {
        var projectId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        decimal? confirmedPrice = null;
        string? quotationBody = null;

        _httpHandler.HandlerFunc = async (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }

            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path == $"/api/v1/projects/{projectId}" && request.Method == HttpMethod.Put)
                return new HttpResponseMessage(HttpStatusCode.NoContent);

            if (path == $"/api/v1/projects/{projectId}/parts/{partId}" && request.Method == HttpMethod.Put)
                return new HttpResponseMessage(HttpStatusCode.NoContent);

            if (path == $"/api/v1/projects/{projectId}/parts/{partId}/confirm-price" && request.Method == HttpMethod.Post)
            {
                var body = await request.Content!.ReadAsStringAsync(ct);
                using var json = JsonDocument.Parse(body);
                confirmedPrice = json.RootElement.GetProperty("confirmedUnitPrice").GetDecimal();
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (path == $"/api/v1/projects/{projectId}/generate-quotation" && request.Method == HttpMethod.Post)
            {
                quotationBody = await request.Content!.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return await DefaultHandler(request, ct);
        };

        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        cut.WaitForAssertion(() =>
            Assert.Contains(_sentRequests, request => request.RequestUri?.AbsolutePath == "/api/v1/pricing/lead-times"));
        ClearRequests();

        var materialId = Guid.NewGuid();
        SetPrivateField(cut.Instance, "_serverProjectId", (Guid?)projectId);
        SetPrivateField(cut.Instance, "_title", "Priced generated quote");
        SetPrivateField(cut.Instance, "_selectedCustomer", new CustomerSummaryDto
        {
            Id = Guid.NewGuid(),
            Name = "Wanasrivwilai Engineering",
            Email = "quote@example.test",
        });
        SetPrivateField(cut.Instance, "_selectedLeadTime", new LeadTimeOptionDto("STANDARD", "Standard", 7, 10, 1m, true));
        GetParts(cut.Instance).Add(new PartViewModel
        {
            FileId = Guid.NewGuid(),
            ServerPartId = partId,
            Name = "priced-part.stl",
            StoragePath = "projects/priced-part.stl",
            ProcessId = Guid.NewGuid(),
            ProcessCode = "CNC_MILL",
            MaterialId = materialId,
            MaterialCode = "AL6061",
            Quantity = 2,
            EstimatedBaseUnitPrice = 1500m,
            EstimatedDiscountedUnitPriceBeforeFinish = 1000m,
            FinishAdditionalUnitCost = 250m,
            EstimatedUnitPrice = 1250m,
            EstimatedTotalAmount = 2500m,
            IsManifold = true,
        });

        Assert.True(GetPrivateProperty<bool>(cut.Instance, "CanSubmit"));
        await InvokePrivateTaskAsync(cut, "CreateProjectAndQuoteAsync");

        var paths = _sentRequests.Select(request => request.RequestUri?.AbsolutePath ?? string.Empty).ToList();
        var confirmIndex = paths.FindIndex(path => path == $"/api/v1/projects/{projectId}/parts/{partId}/confirm-price");
        var quoteIndex = paths.FindIndex(path => path == $"/api/v1/projects/{projectId}/generate-quotation");

        Assert.True(confirmIndex >= 0, $"The quote flow must persist the calculated part price before generating the quotation. Requests: {string.Join(", ", paths)}");
        Assert.True(quoteIndex > confirmIndex, $"The quotation endpoint must run after part prices are confirmed. Requests: {string.Join(", ", paths)}");
        Assert.Equal(1750m, confirmedPrice);
        Assert.NotNull(quotationBody);
        using (var json = JsonDocument.Parse(quotationBody))
        {
            Assert.Equal(30, json.RootElement.GetProperty("validityDays").GetInt32());
            Assert.Contains("Standard", json.RootElement.GetProperty("deliveryExpectations").GetString(), StringComparison.Ordinal);
            Assert.Equal(1000m, json.RootElement.GetProperty("bulkDiscountAmount").GetDecimal());
            Assert.Equal(175m, json.RootElement.GetProperty("taxAmount").GetDecimal());

            var pdfData = json.RootElement.GetProperty("pdfData");
            Assert.Equal("THB", pdfData.GetProperty("currency").GetString());
            Assert.Equal(3500m, pdfData.GetProperty("subtotalBeforeDiscount").GetDecimal());
            Assert.Equal(1000m, pdfData.GetProperty("totalDiscount").GetDecimal());
            Assert.Equal(2500m, pdfData.GetProperty("subtotal").GetDecimal());
            Assert.Equal(175m, pdfData.GetProperty("taxAmount").GetDecimal());
            Assert.Equal(1750m, pdfData.GetProperty("items")[0].GetProperty("unitPrice").GetDecimal());
            Assert.Equal(3500m, pdfData.GetProperty("items")[0].GetProperty("lineTotal").GetDecimal());
        }
        Assert.EndsWith($"/sales/projects/{projectId}", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
        var snackbar = Services.GetRequiredService<ISnackbar>();
        Assert.Contains(snackbar.ShownSnackbars, item => item.Message == "Quotation regenerated.");
        Assert.DoesNotContain(snackbar.ShownSnackbars, item => item.Message == "Project updated and quotation regenerated.");
        Assert.DoesNotContain(snackbar.ShownSnackbars, item => item.Message == "Project and quotation created successfully!");
    }

    [Fact]
    public void CanSubmit_WhenConfiguredPartHasUnacknowledgedDfmIssue_IsFalse()
    {
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        cut.WaitForAssertion(() =>
            Assert.Contains(_sentRequests, request => request.RequestUri?.AbsolutePath == "/api/v1/pricing/lead-times"));

        SetPrivateField(cut.Instance, "_title", "DFM gate quote");
        SetPrivateField(cut.Instance, "_selectedCustomer", new CustomerSummaryDto
        {
            Id = Guid.NewGuid(),
            Name = "Wanasrivwilai Engineering",
            Email = "quote@example.test",
        });
        SetPrivateField(cut.Instance, "_selectedLeadTime", new LeadTimeOptionDto("STANDARD", "Standard", 7, 10, 1m, true));
        var report = new FdmDfmReportPayload(
            ReportType: "FDM",
            ThinWallCount: 1,
            ThinWallRegions: [],
            OverhangFaceCount: 0,
            OverhangAreaCm2: 0,
            OverhangRegions: [],
            SupportRequired: false,
            EstimatedSupportVolumeCm3: null,
            SmallDetailCount: 0,
            Issues: []);
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "thin-wall-part.stl",
            StoragePath = "projects/thin-wall-part.stl",
            ProcessId = Guid.NewGuid(),
            ProcessCode = "FDM",
            MaterialId = Guid.NewGuid(),
            MaterialCode = "PLA",
            Quantity = 2,
            EstimatedBaseUnitPrice = 1250m,
            EstimatedUnitPrice = 1250m,
            EstimatedTotalAmount = 2500m,
            IsManifold = true,
            BodyCount = 1,
            FdmDfmReport = report,
        };
        part.ResolveDfmReport();
        GetParts(cut.Instance).Add(part);

        Assert.True(part.HasProcessRelevantDfmIssues);
        Assert.False(GetPrivateProperty<bool>(cut.Instance, "CanSubmit"));

        part.DfmAcknowledged = true;

        Assert.True(GetPrivateProperty<bool>(cut.Instance, "CanSubmit"));
    }

    [Fact]
    public async Task ApplyAnalysisStatusAsync_WhenPolledDfmReportUsesCamelCase_HydratesTypedReport()
    {
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "polled-dfm.stl",
            StoragePath = "projects/polled-dfm.stl",
            ProcessCode = "FDM",
            AwaitingPreview = true,
            DfmAnalysisTimedOut = true,
            AnalysisErrorCode = "DFM_REPORT_UNAVAILABLE",
        };
        GetParts(cut.Instance).Add(part);

        var fdmReport = new FdmDfmReportPayload(
            ReportType: "FDM",
            ThinWallCount: 0,
            ThinWallRegions: [],
            OverhangFaceCount: 0,
            OverhangAreaCm2: 0,
            OverhangRegions: [],
            SupportRequired: false,
            EstimatedSupportVolumeCm3: null,
            SmallDetailCount: 0,
            Issues: []);
        var status = new FileAnalysisStatusDto
        {
            UploadId = "projects/polled-dfm.stl",
            Status = FileAnalysisStatus.Completed,
            DfmReport = JsonSerializer.SerializeToElement(new
            {
                fdmReport,
                slaReport = (object?)null,
                cncReport = (object?)null,
            }),
            PreviewProcessingStatus = PreviewProcessingStatus.Completed,
        };

        await InvokePrivateTaskWithArgsAsync(cut, "ApplyAnalysisStatusAsync", part, status);

        Assert.Same(part.FdmDfmReport, part.DfmReport);
        Assert.IsType<FdmDfmReportPayload>(part.DfmReport);
        Assert.False(part.DfmAnalysisTimedOut);
        Assert.Null(part.AnalysisErrorCode);
        Assert.False(part.AwaitingPreview);
        Assert.Equal("Ready", part.StatusText);
    }

    [Fact]
    public async Task HandleLocalGeometryRuntimeCompletedAsync_WhenResultMatchesProcess_HydratesDfmReport()
    {
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "local-cnc.stl",
            StoragePath = "projects/local-cnc.stl",
            ProcessCode = "CNC_MILL",
            DfmAnalysisTimedOut = false,
            AnalysisErrorCode = "FILE_MISSING",
        };
        GetParts(cut.Instance).Add(part);

        var result = new LocalGeometryRuntimeResult
        {
            ProcessCode = "CNC_MILL",
            Authority = "local_primary",
            ExecutionMode = "primary_interactive",
            IsAuthoritative = false,
            RuntimeVersion = "1.0.0",
            AlgorithmVersion = "browser-first-dfm-v1",
            InputHash = "abc123",
            Metrics = new LocalGeometryRuntimeMetrics
            {
                FaceCount = 27122,
                VolumeMm3 = 12500,
                IsManifold = false,
                NonManifoldEdgeCount = 8,
                BoundingBox = new LocalGeometryRuntimeBoundingBox
                {
                    X = 25,
                    Y = 20,
                    Z = 10,
                },
            },
            Issues =
            [
                new LocalGeometryRuntimeIssue
                {
                    Category = "mesh_integrity",
                    Severity = "warning",
                    Title = "Mesh may be non-manifold",
                    Description = "Local analysis found boundary edges.",
                    Value = 12,
                    Threshold = 0,
                    FaceIndices = [1, 2, 3],
                    Centroid = [1.0, 2.0, 3.0],
                },
            ],
        };

        var applied = await InvokePrivateTaskWithResultAsync<bool>(cut, "HandleLocalGeometryRuntimeCompletedAsync", new PartLocalGeometryRuntimeResult
        {
            Part = part,
            Result = result,
        });

        Assert.True(applied);
        var report = Assert.IsType<DfmReport>(part.DfmReport);
        Assert.Same(report, part.CncDfmReport);
        Assert.Equal("CNC_MILL", report.ReportType);
        var issue = Assert.Single(report.Issues);
        Assert.Equal("mesh_integrity", issue.Category);
        Assert.Equal([1, 2, 3], issue.FaceIndices);
        Assert.Equal(12500, part.VolumeMm3);
        Assert.NotNull(part.Dimensions);
        Assert.Equal(25, part.Dimensions.X);
        Assert.Equal(20, part.Dimensions.Y);
        Assert.Equal(10, part.Dimensions.Z);
        Assert.Equal(12500, part.Dimensions.VolumeMm3);
        Assert.False(part.IsManifold);
        Assert.Equal(8, part.NonManifoldFaceCount);
        Assert.Equal("Found 8 non-manifold edge(s) shared by more than two faces.", part.NonManifoldReason);
        Assert.False(part.DfmAnalysisTimedOut);
        Assert.Null(part.AnalysisErrorCode);
    }

    [Fact]
    public async Task HandleLocalGeometryRuntimeCompletedAsync_WhenStepAssemblyReportsBrowserEdgeNoise_KeepsCadManifoldAndBodies()
    {
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "Housing for Drive wheel.STEP",
            StoragePath = "projects/housing.step",
            ViewerStoragePath = "projects/housing.step",
            ViewerFileExtension = ".step",
            ProcessCode = "CNC_MILL",
            IsManifold = true,
        };
        GetParts(cut.Instance).Add(part);

        var result = new LocalGeometryRuntimeResult
        {
            ProcessCode = "CNC_MILL",
            Authority = "local_primary",
            ExecutionMode = "primary_interactive",
            IsAuthoritative = false,
            RuntimeVersion = "1.0.0",
            AlgorithmVersion = "browser-first-dfm-v1",
            InputHash = "step123",
            Metrics = new LocalGeometryRuntimeMetrics
            {
                FaceCount = 84520,
                VolumeMm3 = 200000,
                IsManifold = false,
                NonManifoldEdgeCount = 561,
                BodyCount = 3,
                BoundingBox = new LocalGeometryRuntimeBoundingBox
                {
                    X = 60,
                    Y = 33,
                    Z = 60,
                },
            },
            Issues =
            [
                new LocalGeometryRuntimeIssue
                {
                    Category = "mesh_integrity",
                    Severity = "warning",
                    Title = "Non-manifold mesh",
                    Description = "Found 561 non-manifold edges.",
                    Value = 561,
                    Threshold = 0,
                },
            ],
        };

        var applied = await InvokePrivateTaskWithResultAsync<bool>(cut, "HandleLocalGeometryRuntimeCompletedAsync", new PartLocalGeometryRuntimeResult
        {
            Part = part,
            Result = result,
        });

        Assert.True(applied);
        Assert.True(part.IsManifold);
        Assert.Null(part.NonManifoldReason);
        Assert.Null(part.NonManifoldFaceCount);
        Assert.Equal(3, part.BodyCount);
        Assert.Equal(3, part.Bodies.Count);
        Assert.Equal(["Body 1", "Body 2", "Body 3"], part.Bodies.Select(body => body.Name).ToArray());

        var report = Assert.IsType<DfmReport>(part.DfmReport);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public async Task HandleLocalGeometryRuntimeCompletedAsync_WhenResultDoesNotMatchProcess_ReturnsFalse()
    {
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "local-fdm.stl",
            StoragePath = "projects/local-fdm.stl",
            ProcessCode = "FDM",
            AnalysisErrorCode = "FILE_MISSING",
        };
        GetParts(cut.Instance).Add(part);

        var result = new LocalGeometryRuntimeResult
        {
            ProcessCode = "CNC_MILL",
            Authority = "local_primary",
            ExecutionMode = "primary_interactive",
            IsAuthoritative = false,
        };

        var applied = await InvokePrivateTaskWithResultAsync<bool>(cut, "HandleLocalGeometryRuntimeCompletedAsync", new PartLocalGeometryRuntimeResult
        {
            Part = part,
            Result = result,
        });

        Assert.False(applied);
        Assert.Null(part.DfmReport);
        Assert.Equal("FILE_MISSING", part.AnalysisErrorCode);
    }

    [Fact]
    public async Task HandleLocalGeometryRuntimeStartedAsync_WhenResultMatchesProcess_MarksBrowserRuntimeRunning()
    {
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "local-started.stl",
            StoragePath = "projects/local-started.stl",
            ProcessCode = "CNC_MILL",
            LocalDfmRuntimeTerminalProcessCode = "CNC_MILL",
            LocalDfmRuntimeTerminalReason = "worker_failed",
        };
        GetParts(cut.Instance).Add(part);

        await InvokePrivateTaskWithArgsAsync(cut, "HandleLocalGeometryRuntimeStartedAsync", new PartLocalGeometryRuntimeStarted
        {
            Part = part,
            Result = new LocalGeometryRuntimeStarted
            {
                ProcessCode = "CNC_MILL",
            },
        });

        Assert.Equal("CNC_MILL", part.LocalDfmRuntimeRunningProcessCode);
        Assert.NotNull(part.LocalDfmRuntimeStartedAtUtc);
        Assert.Null(part.LocalDfmRuntimeTerminalProcessCode);
        Assert.Null(part.LocalDfmRuntimeTerminalReason);
        Assert.Null(part.AnalysisErrorCode);
        Assert.False(part.DfmAnalysisTimedOut);
    }

    [Fact]
    public async Task HandleLocalGeometryRuntimeUnavailableAsync_WhenResultMatchesProcess_ReleasesLocalDfmWait()
    {
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "local-large.stl",
            StoragePath = "projects/local-large.stl",
            ProcessCode = "CNC_MILL",
        };
        GetParts(cut.Instance).Add(part);

        await InvokePrivateTaskWithArgsAsync(cut, "HandleLocalGeometryRuntimeUnavailableAsync", new PartLocalGeometryRuntimeUnavailable
        {
            Part = part,
            Result = new LocalGeometryRuntimeUnavailable
            {
                ProcessCode = "CNC_MILL",
                Reason = "input_too_large",
            },
        });

        Assert.Equal("CNC_MILL", part.LocalDfmRuntimeTerminalProcessCode);
        Assert.Equal("input_too_large", part.LocalDfmRuntimeTerminalReason);
        Assert.Null(part.DfmReport);
        Assert.Null(part.AnalysisErrorCode);
        Assert.False(part.DfmAnalysisTimedOut);
    }

    [Fact]
    public async Task ApplyFileAnalysisCompletedPayloadAsync_WhenPreviewUrlResolutionFails_PreservesThumbnail()
    {
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "hero-3d-compressed.glb",
            StoragePath = "projects/uploads/hero-3d-compressed.glb",
            AwaitingPreview = true,
            StatusText = "Analyze your model..."
        };
        GetParts(cut.Instance).Add(part);

        var payload = new global::Maliev.Intranet.Client.SignalRFileAnalysisPayload
        {
            StoragePath = "projects/uploads/hero-3d-compressed.glb",
            Failed = true,
            ErrorCode = "preview-url-resolution-failed",
            PreviewUrls = new global::Maliev.Intranet.Client.SignalRPreviewUrls
            {
                ThumbnailSmall = "https://signed.example/thumb-small.webp",
                ThumbnailLarge = "https://signed.example/thumb-large.webp",
                ThumbnailSmallGcsPath = "projects/uploads/hero-3d-compressed.glb_thumbnail_small.webp",
                ThumbnailLargeGcsPath = "projects/uploads/hero-3d-compressed.glb_thumbnail_large.webp"
            }
        };

        await InvokePrivateTaskWithArgsAsync(cut, "ApplyFileAnalysisCompletedPayloadAsync", payload);

        Assert.Equal("https://signed.example/thumb-small.webp", part.ThumbnailSmallUrl);
        Assert.Equal("https://signed.example/thumb-large.webp", part.ThumbnailLargeUrl);
        Assert.Equal("projects/uploads/hero-3d-compressed.glb_thumbnail_small.webp", part.ThumbnailSmallGcsPath);
        Assert.Equal("projects/uploads/hero-3d-compressed.glb_thumbnail_large.webp", part.ThumbnailLargeGcsPath);
        Assert.False(part.AwaitingPreview);
        Assert.False(part.DfmAnalysisTimedOut);
        Assert.Null(part.AnalysisErrorCode);
        Assert.Equal("Ready", part.StatusText);
    }

    [Fact]
    public void SaveDraftToServerAsync_FirstSave_PostsToProjectsEndpoint()
    {
        var createRequestId = Guid.NewGuid();
        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("projects") && request.Method == HttpMethod.Post
                && !path.Contains("parts") && !path.Contains("quotation"))
            {
                var created = new ProjectDetailDto
                {
                    Id = createRequestId,
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "Test",
                    Title = "New Project",
                    Status = "Draft",
                    Currency = "THB",
                    Parts = [],
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(created), Encoding.UTF8, "application/json")
                });
            }
            return DefaultHandler(request, ct);
        };
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        Assert.NotNull(cut.Instance);
    }

    [Fact]
    public void SaveDraftToServerAsync_WhenCreateFails_ComponentRendersWithoutError()
    {
        _httpHandler.HandlerFunc = (request, ct) =>
        {
            lock (_sentRequests) { _sentRequests.Add(request); }
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("projects") && request.Method == HttpMethod.Post
                && !path.Contains("parts") && !path.Contains("quotation"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
            return DefaultHandler(request, ct);
        };
        var cut = Render<global::Maliev.Intranet.Client.Pages.ProjectNew>();
        Assert.NotNull(cut.Instance);
    }

    [Fact]
    public void ApplyCatalogDefaults_WhenCncTurningCatalogLoads_SelectsMediumToleranceAndRoughness()
    {
        var mediumTolerance = new CatalogToleranceDto(
            Guid.NewGuid(),
            "Medium ISO 2768-m",
            "ISO2768_M",
            "ISO 2768",
            "m",
            null,
            0m,
            20);
        var fineTolerance = new CatalogToleranceDto(
            Guid.NewGuid(),
            "Fine ISO 2768-f",
            "ISO2768_F",
            "ISO 2768",
            "f",
            null,
            10m,
            10);
        var part = new PartViewModel
        {
            ProcessCode = "CNC_TURN",
            AvailableTolerances = [fineTolerance, mediumTolerance],
        };

        ApplyCatalogDefaults(part);

        Assert.Equal(mediumTolerance.Id, part.ToleranceId);
        Assert.Equal(mediumTolerance.Code, part.ToleranceCode);
        Assert.Equal("RA_3_2", part.RoughnessCode);
    }

    [Fact]
    public void NormalizeSelectedMaterialForClearPetg_WhenClearPetgSelected_MapsToBasePetgAndSetsClearColor()
    {
        var petgId = Guid.NewGuid();
        var clearPetgId = Guid.NewGuid();
        var part = new PartViewModel
        {
            MaterialId = clearPetgId,
            AvailableMaterials =
            [
                new CatalogMaterialDto(petgId, "PETG", "PETG", "Polymer", null, "Durable, slightly flexible.", 110),
                new CatalogMaterialDto(clearPetgId, "Clear PETG", "PETG_CLEAR", "Polymer", null, "Translucent clear PETG.", 115),
            ],
            ProcessOptionValues = [],
        };

        NormalizeSelectedMaterialForClearPetg(part);

        Assert.Equal(petgId, part.MaterialId);
        Assert.Equal("PETG", part.MaterialCode);
        Assert.Equal("Clear", part.ProcessOptionValues["material_color"]);
    }

    [Fact]
    public void NormalizeSelectedMaterialForClearPetg_WhenBasePetgSelected_DoesNotForceClearColor()
    {
        var petgId = Guid.NewGuid();
        var part = new PartViewModel
        {
            MaterialId = petgId,
            MaterialCode = "PETG",
            AvailableMaterials =
            [
                new CatalogMaterialDto(petgId, "PETG", "PETG", "Polymer", null, "Durable, slightly flexible.", 110),
            ],
            ProcessOptionValues = [],
        };

        NormalizeSelectedMaterialForClearPetg(part);

        Assert.Equal(petgId, part.MaterialId);
        Assert.Equal("PETG", part.MaterialCode);
        Assert.False(part.ProcessOptionValues.ContainsKey("material_color"));
    }

    [Fact]
    public void TotalWeightKg_UsesSelectedCatalogMaterialDensity()
    {
        var aluminumId = Guid.NewGuid();
        var nylonId = Guid.NewGuid();
        var page = new global::Maliev.Intranet.Client.Pages.ProjectNew();
        GetParts(page).Add(new PartViewModel
        {
            Name = "aluminum-bracket.step",
            VolumeMm3 = 1_000,
            MaterialId = aluminumId,
            AvailableMaterials =
            [
                new CatalogMaterialDto(aluminumId, "Aluminum 6061", "AL6061", "Metal", 2.70m, null, 10),
            ],
        });
        GetParts(page).Add(new PartViewModel
        {
            Name = "nylon-cover.stl",
            VolumeMm3 = 1_000,
            MaterialId = nylonId,
            AvailableMaterials =
            [
                new CatalogMaterialDto(nylonId, "PA12 Nylon", "PA12", "Plastic", 1.24m, null, 10),
            ],
        });

        var totalWeightKg = GetPrivateProperty<decimal>(page, "TotalWeightKg");

        Assert.Equal(0.00394m, totalWeightKg);
    }

    [Fact]
    public void ApplyReplacementFileToPart_PreservesConfigurationAndResetsGeometryState()
    {
        var processId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var finishId = Guid.NewGuid();
        var toleranceId = Guid.NewGuid();
        var newUploadId = Guid.NewGuid();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "old-bracket.step",
            StoragePath = "projects/project-1/old-bracket.step",
            ProcessCode = "CNC_MILL",
            ProcessId = processId,
            MaterialCode = "AL6061",
            MaterialId = materialId,
            FinishCode = "ANODIZED",
            FinishId = finishId,
            ToleranceCode = "ISO2768_M",
            ToleranceId = toleranceId,
            Quantity = 12,
            PartNotes = "Keep cosmetic face A.",
            Dimensions = new FileAnalysisDimensionsDto { X = 10, Y = 20, Z = 30 },
            VolumeMm3 = 456,
            SurfaceAreaMm2 = 789,
            DfmReport = new DfmReport { ReportType = "CNC_MILL" },
            GlbStoragePath = "projects/project-1/old-bracket.step_viewer.glb",
            ViewerUrl = "https://signed.example/old.glb",
            OverlayUrls = new Dictionary<string, string> { ["CNC_MILL__sharp_corner"] = "https://signed.example/overlay.glb" },
        };
        var completed = new BffUploadResponse
        {
            UploadId = newUploadId.ToString(),
            FileName = "new-bracket.step",
            FileSize = 987_000,
            StoragePath = "projects/project-1/new-bracket.step",
        };

        InvokePrivateStaticVoid(
            typeof(global::Maliev.Intranet.Client.Pages.ProjectNew),
            "ApplyReplacementFileToPart",
            part,
            "new-bracket.step",
            completed,
            "revision-client-id");

        Assert.Equal(newUploadId, part.FileId);
        Assert.Equal("new-bracket.step", part.Name);
        Assert.Equal("projects/project-1/new-bracket.step", part.StoragePath);
        Assert.Equal("revision-client-id", part.ClientUploadId);
        Assert.Equal(newUploadId.ToString("N"), part.ThumbnailVersion);

        Assert.Equal("CNC_MILL", part.ProcessCode);
        Assert.Equal(processId, part.ProcessId);
        Assert.Equal("AL6061", part.MaterialCode);
        Assert.Equal(materialId, part.MaterialId);
        Assert.Equal("ANODIZED", part.FinishCode);
        Assert.Equal(finishId, part.FinishId);
        Assert.Equal("ISO2768_M", part.ToleranceCode);
        Assert.Equal(toleranceId, part.ToleranceId);
        Assert.Equal(12, part.Quantity);
        Assert.Equal("Keep cosmetic face A.", part.PartNotes);

        Assert.Null(part.Dimensions);
        Assert.Null(part.VolumeMm3);
        Assert.Null(part.SurfaceAreaMm2);
        Assert.Null(part.DfmReport);
        Assert.Null(part.GlbStoragePath);
        Assert.Null(part.ViewerUrl);
        Assert.Null(part.OverlayUrls);
        Assert.False(part.DfmAnalysisTimedOut);
        Assert.Null(part.AnalysisErrorCode);
    }

    [Fact]
    public void RefreshLeadTimeOptionsFromPricing_WhenNoLeadTimeSelected_SelectsStandardWithBufferedRanges()
    {
        var page = new global::Maliev.Intranet.Client.Pages.ProjectNew();
        SetPrivateField(page, "_leadTimeCatalogOptions", new List<LeadTimeOptionDto>
        {
            new("ECONOMY", "Economy", 0, 0, 0.9m, false),
            new("STANDARD", "Standard", 0, 0, 1.0m, true),
            new("EXPRESS", "Express", 0, 0, 1.3m, false),
        });
        GetParts(page).Add(new PartViewModel
        {
            EstimatedLeadTimeDays = 7,
        });

        InvokePrivateVoid(page, "RefreshLeadTimeOptionsFromPricing");

        var options = GetPrivateField<List<LeadTimeOptionDto>>(page, "_leadTimeOptions");
        var selected = GetPrivateField<LeadTimeOptionDto?>(page, "_selectedLeadTime");

        Assert.Equal("STANDARD", selected?.Code);
        Assert.Contains(options, option => option.Code == "ECONOMY" && option.MinDays == 9 && option.MaxDays == 12);
        Assert.Contains(options, option => option.Code == "STANDARD" && option.MinDays == 7 && option.MaxDays == 10);
        Assert.Contains(options, option => option.Code == "EXPRESS" && option.MinDays == 4 && option.MaxDays == 6);
    }

    private static async Task InvokeHandleFileSelectedAsync(
        RenderedComponent<global::Maliev.Intranet.Client.Pages.ProjectNew> cut,
        IReadOnlyList<IBrowserFile> files)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetMethod("HandleFileSelected", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, [files])!);
        await result;
    }

    private static async Task InvokePrivateTaskAsync(
        RenderedComponent<global::Maliev.Intranet.Client.Pages.ProjectNew> cut,
        string methodName)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, [])!);
        await result;
    }

    private static async Task InvokePrivateTaskWithArgsAsync(
        RenderedComponent<global::Maliev.Intranet.Client.Pages.ProjectNew> cut,
        string methodName,
        params object[] args)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = cut.InvokeAsync(() => (Task)method.Invoke(cut.Instance, args)!);
        await result;
    }

    private static async Task<T> InvokePrivateTaskWithResultAsync<T>(
        RenderedComponent<global::Maliev.Intranet.Client.Pages.ProjectNew> cut,
        string methodName,
        params object[] args)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        return await cut.InvokeAsync(() => (Task<T>)method.Invoke(cut.Instance, args)!);
    }

    private static List<PartViewModel> GetParts(global::Maliev.Intranet.Client.Pages.ProjectNew instance)
    {
        var field = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetField("_parts", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        return Assert.IsType<List<PartViewModel>>(field.GetValue(instance));
    }

    private static void ApplyCatalogDefaults(PartViewModel part)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetMethod("ApplyCatalogDefaults", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(null, [part]);
    }

    private static void NormalizeSelectedMaterialForClearPetg(PartViewModel part)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetMethod("NormalizeSelectedMaterialForClearPetg", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(null, [part]);
    }

    private static void InvokePrivateVoid(
        global::Maliev.Intranet.Client.Pages.ProjectNew instance,
        string methodName)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(instance, []);
    }

    private static void InvokePrivateStaticVoid(
        Type type,
        string methodName,
        params object[] args)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(null, args);
    }

    private static PartViewModel CreatePartViewModelFromProjectPart(
        RenderedComponent<global::Maliev.Intranet.Client.Pages.ProjectNew> cut,
        ProjectPartDto part)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetMethod("CreatePartViewModelFromProjectPart", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        return Assert.IsType<PartViewModel>(method.Invoke(cut.Instance, [part]));
    }

    private static UpdateProjectPartRequest? BuildUpdateProjectPartRequest(
        global::Maliev.Intranet.Client.Pages.ProjectNew instance,
        PartViewModel part)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetMethod("BuildUpdateProjectPartRequest", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        return Assert.IsType<UpdateProjectPartRequest?>(method.Invoke(instance, [part]));
    }

    private static void SetPrivateField<T>(
        global::Maliev.Intranet.Client.Pages.ProjectNew instance,
        string fieldName,
        T value)
    {
        var field = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(instance, value);
    }

    private static T GetPrivateField<T>(
        global::Maliev.Intranet.Client.Pages.ProjectNew instance,
        string fieldName)
    {
        var field = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<T>(field.GetValue(instance));
    }

    private static T GetPrivateProperty<T>(
        global::Maliev.Intranet.Client.Pages.ProjectNew instance,
        string propertyName)
    {
        var property = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<T>(property.GetValue(instance));
    }

    private static HttpResponseMessage CreateResumableSessionResponse()
    {
        var session = new BffResumableUploadSessionResponse
        {
            UploadId = Guid.NewGuid().ToString("N"),
            SessionUri = "https://storage.example/upload-session",
            StoragePath = $"projects/{Guid.NewGuid()}/part.stl",
            FileName = "part.stl",
            FileSize = 1024,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(session), Encoding.UTF8, "application/json")
        };
    }

    private sealed class TestBrowserFile(string name, long size, string contentType = "model/stl") : IBrowserFile
    {
        public string Name { get; } = name;

        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

        public long Size { get; } = size;

        public string ContentType { get; } = contentType;

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("ProjectNew upload tests must not read browser file streams.");
        }
    }
}

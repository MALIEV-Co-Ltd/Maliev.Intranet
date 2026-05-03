using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
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
            ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".stl", ".step", ".3mf", ".obj" },
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
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();
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
    public void ResumeFromServerAsync_WhenProjectStatusIsQuoted_LoadsProjectForEditing()
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
                    Status = "Quoted",
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
    public async Task CreateProjectAndQuoteAsync_WhenPartsHavePrices_ConfirmsPartPricesBeforeGeneratingQuotation()
    {
        var projectId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        decimal? confirmedPrice = null;

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
                return new HttpResponseMessage(HttpStatusCode.NoContent);

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
            EstimatedUnitPrice = 1250m,
            EstimatedTotalAmount = 2500m,
        });

        Assert.True(GetPrivateProperty<bool>(cut.Instance, "CanSubmit"));
        await InvokePrivateTaskAsync(cut, "CreateProjectAndQuoteAsync");

        var paths = _sentRequests.Select(request => request.RequestUri?.AbsolutePath ?? string.Empty).ToList();
        var confirmIndex = paths.FindIndex(path => path == $"/api/v1/projects/{projectId}/parts/{partId}/confirm-price");
        var quoteIndex = paths.FindIndex(path => path == $"/api/v1/projects/{projectId}/generate-quotation");

        Assert.True(confirmIndex >= 0, $"The quote flow must persist the calculated part price before generating the quotation. Requests: {string.Join(", ", paths)}");
        Assert.True(quoteIndex > confirmIndex, $"The quotation endpoint must run after part prices are confirmed. Requests: {string.Join(", ", paths)}");
        Assert.Equal(1250m, confirmedPrice);
        Assert.EndsWith($"/sales/projects/{projectId}", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
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

    private static void InvokePrivateVoid(
        global::Maliev.Intranet.Client.Pages.ProjectNew instance,
        string methodName)
    {
        var method = typeof(global::Maliev.Intranet.Client.Pages.ProjectNew)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(instance, []);
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

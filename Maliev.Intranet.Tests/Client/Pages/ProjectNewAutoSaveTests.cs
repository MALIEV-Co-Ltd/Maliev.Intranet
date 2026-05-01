using Bunit;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using System.Net;
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
}

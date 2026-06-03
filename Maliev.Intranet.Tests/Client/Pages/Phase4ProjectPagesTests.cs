using Bunit;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 4: Project Lifecycle Pages — Projects list, ProjectNew, ProjectDetail.
/// </summary>
public class Phase4ProjectPagesTests : BunitContext, IAsyncLifetime
{
    public Phase4ProjectPagesTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler();
        handler.HandlerFunc = (request, _) =>
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("currencies"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[{\"Code\":\"THB\",\"Name\":\"Thai Baht\",\"Symbol\":\"฿\"}]", Encoding.UTF8, "application/json")
                });
            if (path.Contains("processes"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                });
            if (path.Contains("lead-times"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json")
                });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        };

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddScoped<BreadcrumbService>();

        var draftServiceMock = new Mock<IProjectDraftService>();
        draftServiceMock.Setup(s => s.LoadDraftAsync(It.IsAny<string?>())).ReturnsAsync((DraftProjectState?)null);
        draftServiceMock.Setup(s => s.SaveDraftAsync(It.IsAny<DraftProjectState>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        draftServiceMock.Setup(s => s.ClearDraftAsync(It.IsAny<string?>())).Returns(Task.CompletedTask);
        Services.AddSingleton(draftServiceMock.Object);

        Services.AddLogging();
        Services.AddAuthorization();
        Services.AddScoped<AuthenticationStateProvider, TestAuthenticationStateProvider>();

        var layoutLoggerMock = new Mock<ILogger<LayoutService>>();
        Services.AddSingleton<LayoutService>(new LayoutService(JSInterop.JSRuntime, layoutLoggerMock.Object, null!));

        Services.AddSingleton(new FileTypesSettings
        {
            ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".stl", ".step", ".3mf", ".obj" },
            DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".dxf", ".dwg" },
            ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" },
            OfficeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".doc", ".docx", ".xls", ".xlsx" },
            ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".zip", ".rar", ".7z" },
            DrawingExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".dxf", ".dwg", ".png", ".jpg" },
            SupplementaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".doc", ".docx", ".xls", ".xlsx", ".zip" },
        });

        Services.AddAuthorizationCore();
        Services.AddCascadingAuthenticationState();
        var authServiceMock = new Mock<IAuthorizationService>();
        authServiceMock.Setup(x => x.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(),
            It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        authServiceMock.Setup(x => x.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());
        Services.AddSingleton(authServiceMock.Object);

        Services.AddSingleton<CookieProvider>();
        Services.AddSingleton<ChatService>();
        Services.AddSingleton(new UploadSettings());
        Services.AddScoped<CurrencyService>();
        Services.AddScoped<ShippingService>();
        Services.AddScoped<AlertService>();

        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    // ── Projects list page ────────────────────────────────────────────────────

    [Fact]
    public void ProjectsPage_ShouldRender_WithoutException()
    {
        var cut = Render<Projects>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void ProjectsPage_ShouldContain_NewProjectButton()
    {
        var cut = Render<Projects>();
        Assert.Contains("New Project", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectsPage_ShouldShowLoadingOrGrid()
    {
        var cut = Render<Projects>();
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("mud-skeleton", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("mud-data-grid", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("No projects", StringComparison.OrdinalIgnoreCase),
            "Expected loading state, data grid, or empty state");
    }

    [Fact]
    public void ProjectsPage_ShouldContain_SearchInput()
    {
        var cut = Render<Projects>();
        Assert.Contains("Search", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    // ── ProjectNew page ───────────────────────────────────────────────────────

    [Fact]
    public void ProjectNewPage_ShouldRender_WithoutException()
    {
        var cut = Render<ProjectNew>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void ProjectNewPage_ShouldContain_TitleField()
    {
        var cut = Render<ProjectNew>();
        Assert.Contains("Title", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

}

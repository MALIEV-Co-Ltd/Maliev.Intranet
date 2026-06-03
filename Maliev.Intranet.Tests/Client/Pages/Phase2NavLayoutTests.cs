using Bunit;
using Maliev.Intranet.Client.Layout;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Tests.Testing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 2: Navigation &amp; Layout Overhaul.
/// Verifies the top navigation renders the flat employee modules and breadcrumbs work.
/// </summary>
public class Phase2NavLayoutTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<AuthenticationStateProvider> _authMock = new();

    public Phase2NavLayoutTests()
    {
        Services.AddMudServices();
        Services.AddAuthorizationCore();
        Services.AddCascadingAuthenticationState();
        Services.AddSingleton<AuthenticationStateProvider>(_authMock.Object);

        var authServiceMock = new Mock<IAuthorizationService>();
        authServiceMock.Setup(x => x.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(),
            It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        authServiceMock.Setup(x => x.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());
        Services.AddSingleton(authServiceMock.Object);

        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);

        var layoutLoggerMock = new Mock<ILogger<LayoutService>>();
        var layoutService = new LayoutService(JSInterop.JSRuntime, layoutLoggerMock.Object, null!);
        Services.AddSingleton<LayoutService>(layoutService);
        Services.AddSingleton<ChatService>();
        Services.AddScoped<BreadcrumbService>();
        Services.AddLogging();
        Services.AddScoped<CurrencyService>();
        Services.AddScoped<AlertService>();

        _authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], "Test"))));

        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void TopBar_ShouldRender_WithoutException()
    {
        var cut = Render<TopBar>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void TopBar_ShouldContain_QuoteLink()
    {
        var cut = Render<TopBar>();
        Assert.Contains("sales/projects/new", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBar_ShouldPrioritizeQuoteAsFirstModuleLink()
    {
        var cut = Render<TopBar>();
        var firstLink = cut.Find("nav.topbar-nav a");

        Assert.Contains("Quote", firstLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("sales/projects/new", firstLink.GetAttribute("href"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBar_ShouldStyleQuoteAsPrimaryNavigationAction()
    {
        var cut = Render<TopBar>();
        Assert.Contains("topbar-nav-quote", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBar_LogoLink_PointsToApplicationHome()
    {
        var cut = Render<TopBar>();
        var logo = cut.Find("a.topbar-logo-button");

        Assert.Equal("/", logo.GetAttribute("href"));
        Assert.Equal("Go to application home", logo.GetAttribute("aria-label"));
    }

    [Fact]
    public void TopBar_ShouldContain_FlatModuleLinks()
    {
        var cut = Render<TopBar>();
        cut.Find("button.topbar-mobile-menu-button").Click();

        Assert.Contains("href=\"customers\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"accounting\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"purchasing\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"admin\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"iam\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sales/customers", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBar_MobileNavigationLinks_UseStableIconAndLabelRows()
    {
        var cut = Render<TopBar>();
        cut.Find("button.topbar-mobile-menu-button").Click();

        var mobileLinks = cut.FindAll("nav.topbar-mobile-nav-list a.topbar-mobile-nav-link");
        Assert.NotEmpty(mobileLinks);
        Assert.All(mobileLinks, link =>
        {
            Assert.NotNull(link.QuerySelector(".topbar-mobile-nav-icon"));
            Assert.NotNull(link.QuerySelector(".topbar-mobile-nav-label"));
        });

        var groupTitles = cut.FindAll(".topbar-mobile-nav-group-title");
        Assert.NotEmpty(groupTitles);
        Assert.All(groupTitles, title =>
        {
            Assert.NotNull(title.QuerySelector(".topbar-mobile-nav-group-icon"));
            Assert.NotNull(title.QuerySelector(".topbar-mobile-nav-group-label"));
        });
    }

    [Fact]
    public void TopBar_ShouldContain_ServiceManagementNavigation()
    {
        var cut = Render<TopBar>();
        cut.Find("button.topbar-mobile-menu-button").Click();

        Assert.Contains("commerce/catalog", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin/web-content", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin/web-content?section=blog", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mfg/materials", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin/reference-data?section=countries", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin/reference-data?section=currencies", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin/reference-data?section=registry", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("finance/delivery-notes/new", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin/chatbot-instructions", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sidebar_ShouldUseSharedNavigationCatalog()
    {
        var cut = Render<NavMenu>();

        Assert.Contains("commerce/catalog", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin/web-content", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin/web-content?section=homepage", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin/reference-data?section=exchange-rates", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("purchasing/suppliers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hr/profile?tab=preferences", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBar_ShouldNotExpose_DisplayTweaks()
    {
        var cut = Render<TopBar>();
        Assert.DoesNotContain("Display tweaks", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Accent color", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBar_ProfileButton_Click_ShowsEmployeeActions()
    {
        var cut = Render<TopBar>();

        cut.Find("button.topbar-profile").Click();

        var menu = cut.Find(".topbar-profile-popover");
        Assert.Equal("true", cut.Find("button.topbar-profile").GetAttribute("aria-expanded"));
        Assert.Contains("My Profile", menu.TextContent, StringComparison.Ordinal);
        Assert.Contains("Preferences", menu.TextContent, StringComparison.Ordinal);
        Assert.Contains("Sign out", menu.TextContent, StringComparison.Ordinal);
        Assert.Contains("/hr/profile", menu.QuerySelector("a")?.GetAttribute("href"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBar_ProfileSubtitle_DoesNotDisplayPlatformOwnerRoleClaim()
    {
        _authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity([
                    new Claim(ClaimTypes.Name, "Natthapol Vanasrivilai"),
                    new Claim(ClaimTypes.Role, "roles.platform.owner")
                ], "Test"))));

        var cut = Render<TopBar>();

        var subtitle = cut.Find(".topbar-profile-role").TextContent;
        Assert.Equal("Employee", subtitle);
        Assert.DoesNotContain("Platform Owner", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_ProfileAvatar_FallsBackToInitialsWhenPictureFails()
    {
        _authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity([
                    new Claim(ClaimTypes.Name, "Natthapol Vanasrivilai"),
                    new Claim("picture", "https://localhost/avatar.png")
                ], "Test"))));

        var cut = Render<TopBar>();

        Assert.NotEmpty(cut.FindAll("img.topbar-avatar-image"));
        cut.Find("img.topbar-avatar-image").TriggerEvent("onerror", EventArgs.Empty);

        Assert.Empty(cut.FindAll("img.topbar-avatar-image"));
        Assert.Equal("NV", cut.Find(".topbar-avatar-initials").TextContent.Trim());
        Assert.Null(cut.Find(".topbar-avatar-initials").GetAttribute("hidden"));
    }

    [Fact]
    public void TopBar_ProfileRole_FallsBackToEmployee()
    {
        var cut = Render<TopBar>();

        Assert.Contains("Employee", cut.Find(".topbar-profile-role").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_ProfileInfo_IsLeftAligned()
    {
        var css = File.ReadAllText(FindSourceFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css"));

        Assert.Contains("align-items: flex-start", css, StringComparison.Ordinal);
        Assert.Contains("text-align: left", css, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_SourceCss_PreservesCompactCurrencyAndPrimaryQuoteSelectors()
    {
        var css = File.ReadAllText(FindSourceFile("Maliev.Intranet.Client", "Layout", "TopBar.razor.css"));

        Assert.Contains("box-shadow: var(--maliev-shadow-ring);", css, StringComparison.Ordinal);
        Assert.Contains(".topbar-nav ::deep .mud-nav-link.topbar-nav-quote", css, StringComparison.Ordinal);
        Assert.Contains(".topbar-nav ::deep .topbar-nav-quote .mud-nav-link", css, StringComparison.Ordinal);
        Assert.Contains("background: var(--mud-palette-primary) !important;", css, StringComparison.Ordinal);
        Assert.Contains("border-bottom: 0 !important;", css, StringComparison.Ordinal);
        Assert.Contains("width: 5ch !important;", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".topbar-logo-button:hover", css, StringComparison.Ordinal);
    }

    [Fact]
    public void TopBar_ProfileMenu_SignOut_NavigatesToLogout()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<TopBar>();

        cut.Find("button.topbar-profile").Click();
        cut.FindAll("button.topbar-profile-action")
            .Single(button => button.TextContent.Contains("Sign out", StringComparison.Ordinal))
            .Click();

        Assert.EndsWith("/api/v1/auth/logout", navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BreadcrumbService_SetPageLabel_UpdatesCurrentLabel()
    {
        var svc = new BreadcrumbService();
        svc.SetPageLabel("Acme Corp");
        Assert.Equal("Acme Corp", svc.CurrentLabel);
    }

    [Fact]
    public void BreadcrumbService_Clear_ResetsLabel()
    {
        var svc = new BreadcrumbService();
        svc.SetPageLabel("Test Label");
        svc.Clear();
        Assert.Null(svc.CurrentLabel);
    }

    [Fact]
    public void BreadcrumbService_OnChanged_FiresWhenLabelSet()
    {
        var svc = new BreadcrumbService();
        var fired = false;
        svc.OnChanged += () => fired = true;
        svc.SetPageLabel("Test");
        Assert.True(fired);
    }

    private static string FindSourceFile(params string[] segments)
    {
        return FindSourceFileCore(segments);
    }

    private static string FindSourceFileCore(
        string[] segments,
        [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
    {
        var roots = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
            Path.GetDirectoryName(callerFilePath) ?? string.Empty
        };

        foreach (var root in roots.Where(root => !string.IsNullOrWhiteSpace(root)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var current = new DirectoryInfo(root);
            while (current is not null)
            {
                var candidate = Path.Combine([current.FullName, .. segments]);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate source file.", Path.Combine(segments));
    }
}

using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Unit tests for <see cref="ProjectLeftPanel"/>.
/// Focuses on <c>RefreshRecentProjectsAsync</c> and recent projects loading behavior.
/// </summary>
public class ProjectLeftPanelTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<Func<Guid, Task<List<ProjectSummaryDto>>>> _loadRecentProjectsFuncMock = new();
    private readonly CustomerSummaryDto _testCustomer = new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Customer",
        Email = "test@example.com",
        Status = "Active"
    };

    public ProjectLeftPanelTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
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
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    private RenderedComponent<ProjectLeftPanel> RenderPanel(
        CustomerSummaryDto? selectedCustomer = null,
        bool showCustomerSearch = false,
        Func<Guid, Task<List<ProjectSummaryDto>>>? loadRecentProjectsFunc = null)
    {
        return Render<ProjectLeftPanel>(parameters => parameters
            .Add(p => p.SelectedCustomer, selectedCustomer)
            .Add(p => p.ShowCustomerSearch, showCustomerSearch)
            .Add(p => p.LoadRecentProjectsFunc, loadRecentProjectsFunc));
    }

    [Fact]
    public async Task RefreshRecentProjectsAsync_WhenNoSelectedCustomer_ReturnsWithoutCallingLoadFunc()
    {
        _loadRecentProjectsFuncMock.Setup(f => f(It.IsAny<Guid>()))
            .ReturnsAsync(new List<ProjectSummaryDto>());

        var cut = RenderPanel(
            selectedCustomer: null,
            loadRecentProjectsFunc: _loadRecentProjectsFuncMock.Object);

        await cut.Instance.RefreshRecentProjectsAsync();

        _loadRecentProjectsFuncMock.Verify(f => f(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RefreshRecentProjectsAsync_WhenNoLoadFunc_ReturnsWithoutException()
    {
        var cut = RenderPanel(
            selectedCustomer: _testCustomer,
            loadRecentProjectsFunc: null);

        await cut.Instance.RefreshRecentProjectsAsync();
    }

    [Fact]
    public async Task RefreshRecentProjectsAsync_CallsLoadFuncWithCorrectCustomerId()
    {
        _loadRecentProjectsFuncMock.Setup(f => f(_testCustomer.Id))
            .ReturnsAsync(new List<ProjectSummaryDto>());

        var cut = RenderPanel(
            selectedCustomer: _testCustomer,
            showCustomerSearch: false,
            loadRecentProjectsFunc: _loadRecentProjectsFuncMock.Object);

        await cut.Instance.RefreshRecentProjectsAsync();

        _loadRecentProjectsFuncMock.Verify(f => f(_testCustomer.Id), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RefreshRecentProjectsAsync_BypassesCacheAndReloads()
    {
        var projects = new List<ProjectSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Project A", Status = "Draft", CreatedAt = DateTime.UtcNow }
        };

        var callCount = 0;
        _loadRecentProjectsFuncMock.Setup(f => f(_testCustomer.Id))
            .Callback(() => callCount++)
            .ReturnsAsync(projects);

        var cut = RenderPanel(
            selectedCustomer: _testCustomer,
            showCustomerSearch: false,
            loadRecentProjectsFunc: _loadRecentProjectsFuncMock.Object);

        var initialCount = callCount;

        await cut.Instance.RefreshRecentProjectsAsync();

        Assert.True(callCount >= initialCount + 1,
            $"Expected at least one additional call after refresh. Initial: {initialCount}, Final: {callCount}");
    }

    [Fact]
    public async Task RefreshRecentProjectsAsync_WhenLoadFuncThrows_DoesNotPropagateException()
    {
        _loadRecentProjectsFuncMock.Setup(f => f(_testCustomer.Id))
            .ThrowsAsync(new HttpRequestException("API error"));

        var cut = RenderPanel(
            selectedCustomer: _testCustomer,
            showCustomerSearch: false,
            loadRecentProjectsFunc: _loadRecentProjectsFuncMock.Object);

        await cut.Instance.RefreshRecentProjectsAsync();
    }

    [Fact]
    public async Task RefreshRecentProjectsAsync_LoadsNewProjects()
    {
        var projects = new List<ProjectSummaryDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Recent Project",
                Status = "Draft",
                CreatedAt = DateTime.UtcNow
            }
        };

        _loadRecentProjectsFuncMock.Setup(f => f(_testCustomer.Id))
            .ReturnsAsync(projects);

        var cut = RenderPanel(
            selectedCustomer: _testCustomer,
            showCustomerSearch: false,
            loadRecentProjectsFunc: _loadRecentProjectsFuncMock.Object);

        await cut.Instance.RefreshRecentProjectsAsync();

        cut.Render();

        _loadRecentProjectsFuncMock.Verify(f => f(_testCustomer.Id), Times.AtLeastOnce);
    }
}

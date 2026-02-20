using Bunit;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using Maliev.Intranet.Tests.Testing;
using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace Maliev.Intranet.Tests.Client.Components;

public class Models3DPageTests : BunitContext, IAsyncLifetime
{
    private readonly Mock<ISnackbar> _snackbarMock = new();
    private readonly Mock<IDialogService> _dialogMock = new();
    private readonly MockHttpMessageHandler _httpHandler = new();

    public Models3DPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_snackbarMock.Object);
        Services.AddSingleton(_dialogMock.Object);
        Services.AddSingleton(new Mock<IJSRuntime>().Object);

        var client = new HttpClient(_httpHandler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);

        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ShouldShowEmptyState_WhenNoModels()
    {
        // Arrange
        var response = new PagedResponse<Model3DDto> { Data = new List<Model3DDto>(), Meta = new PaginationMeta { TotalPages = 0 } };
        _httpHandler.HandlerFunc = (req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) });

        // Act
        var cut = Render<Models3D>();

        // Assert
        Assert.Contains("No models found", cut.Markup);
    }

    [Fact]
    public void ShouldShowModels_WhenDataExists()
    {
        // Arrange
        var models = new List<Model3DDto> { new() { Id = Guid.NewGuid(), FileName = "test.stl", UploadedAt = DateTime.Now } };
        var response = new PagedResponse<Model3DDto> { Data = models, Meta = new PaginationMeta { TotalPages = 1 } };
        _httpHandler.HandlerFunc = (req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) });

        // Act
        var cut = Render<Models3D>();

        // Assert
        Assert.Contains("test.stl", cut.Markup);
        Assert.Contains("mud-card", cut.Markup);
    }
}

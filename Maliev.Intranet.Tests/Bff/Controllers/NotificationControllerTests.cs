using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class NotificationControllerTests
{
    private readonly Mock<INotificationServiceClient> _clientMock;
    private readonly NotificationController _controller;

    public NotificationControllerTests()
    {
        _clientMock = new Mock<INotificationServiceClient>();
        _controller = new NotificationController(_clientMock.Object);
    }

    [Fact]
    public async Task GetTemplates_ShouldReturnOk()
    {
        _clientMock.Setup(x => x.GetTemplatesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<NotificationTemplateDto>());

        var result = await _controller.GetTemplates();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTemplate_WhenFound_ReturnsOk()
    {
        _clientMock.Setup(x => x.GetTemplateByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationTemplateDto());

        var result = await _controller.GetTemplate(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTemplate_WhenNotFound_ReturnsNotFound()
    {
        _clientMock.Setup(x => x.GetTemplateByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplateDto?)null);

        var result = await _controller.GetTemplate(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateTemplate_ReturnsOk()
    {
        _clientMock.Setup(x => x.CreateTemplateAsync(It.IsAny<CreateNotificationTemplateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationTemplateDto());

        var result = await _controller.CreateTemplate(new CreateNotificationTemplateRequest(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateTemplate_ReturnsOk()
    {
        _clientMock.Setup(x => x.UpdateTemplateAsync(It.IsAny<Guid>(), It.IsAny<UpdateNotificationTemplateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationTemplateDto());

        var result = await _controller.UpdateTemplate(Guid.NewGuid(), new UpdateNotificationTemplateRequest(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteTemplate_ReturnsNoContent()
    {
        _clientMock.Setup(x => x.DeleteTemplateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteTemplate(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetDeliveryLogs_ReturnsOk()
    {
        _clientMock.Setup(x => x.GetDeliveryLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<NotificationDeliveryLogDto>());

        var result = await _controller.GetDeliveryLogs();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPreferences_WhenFound_ReturnsOk()
    {
        _clientMock.Setup(x => x.GetPreferencesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserNotificationPreferenceDto());

        var result = await _controller.GetPreferences("u1", CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPreferences_WhenNotFound_ReturnsNotFound()
    {
        _clientMock.Setup(x => x.GetPreferencesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotificationPreferenceDto?)null);

        var result = await _controller.GetPreferences("u1", CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePreferences_ReturnsOk()
    {
        _clientMock.Setup(x => x.UpdatePreferencesAsync(It.IsAny<string>(), It.IsAny<UpdateNotificationPreferenceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserNotificationPreferenceDto());

        var result = await _controller.UpdatePreferences("u1", new UpdateNotificationPreferenceRequest(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }
}

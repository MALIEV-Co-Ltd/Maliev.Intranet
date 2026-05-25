using System.Reflection;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Shared;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public sealed class ContactRequestsControllerTests
{
    [Fact]
    public async Task SendReplyAsync_ValidRequest_DispatchesDirectEmailReplyAndMarksResolved()
    {
        var contactClient = new Mock<IContactRequestServiceClient>();
        var notificationClient = new Mock<INotificationServiceClient>();
        NotificationEvent? dispatchedEvent = null;

        contactClient
            .Setup(client => client.GetContactRequestAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContactRequestDto
            {
                Id = 42,
                PublicReference = "MLV-C-000042",
                FullName = "Website Customer",
                Email = "customer@example.com",
                Subject = "Manufacturing question",
                Message = "Can MALIEV review this?",
                Status = ContactRequestStatus.InProgress,
                Priority = ContactRequestPriority.Medium,
                ContactType = ContactRequestType.General,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        contactClient
            .Setup(client => client.UpdateContactStatusAsync(
                42,
                It.Is<ContactRequestStatusUpdateRequest>(request => request.Status == ContactRequestStatus.Resolved),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContactRequestDto { Id = 42, Email = "customer@example.com", Status = ContactRequestStatus.Resolved });
        notificationClient
            .Setup(client => client.DispatchEventAsync(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationEvent, CancellationToken>((notificationEvent, _) => dispatchedEvent = notificationEvent)
            .Returns(Task.CompletedTask);

        var controller = new ContactRequestsController(contactClient.Object, notificationClient.Object);

        var result = await controller.SendReplyAsync(
            42,
            new ContactRequestReplyRequest { ReplyMessage = "Thanks, we are reviewing it.", MarkResolved = true },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ContactRequestReplyResponse>(okResult.Value);
        Assert.True(response.Sent);
        Assert.Equal(ContactRequestStatus.Resolved, response.Status);
        Assert.NotNull(dispatchedEvent);
        Assert.Equal("ContactMessageEmployeeReply", dispatchedEvent.Payload.NotificationType);
        Assert.Equal("contact-message-employee-reply", dispatchedEvent.Payload.TemplateId);
        var targetUser = Assert.Single(dispatchedEvent.Payload.TargetUsers);
        Assert.Equal("direct-email", targetUser.UserType);
        Assert.Equal("contact-message-42-reply", targetUser.UserId);
    }

    [Theory]
    [InlineData(nameof(ContactRequestsController.GetAsync), MalievPermissions.ContactRequest.Read)]
    [InlineData(nameof(ContactRequestsController.GetByIdAsync), MalievPermissions.ContactRequest.Read)]
    [InlineData(nameof(ContactRequestsController.UpdateStatusAsync), MalievPermissions.ContactRequest.Update)]
    [InlineData(nameof(ContactRequestsController.SendReplyAsync), MalievPermissions.ContactRequest.Update)]
    [InlineData(nameof(ContactRequestsController.DownloadFileAsync), MalievPermissions.ContactRequest.Read)]
    public void ContactRequestEndpoints_RequireContactPermissions(string actionName, string expectedPermission)
    {
        var method = typeof(ContactRequestsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName);

        var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>());

        Assert.Contains(expectedPermission, attribute.Policy, StringComparison.Ordinal);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }
}

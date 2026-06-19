using System.Net;
using System.Text;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Consumers;
using Maliev.Intranet.Bff.Data;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Maliev.MessagingContracts.Contracts.Projects;
using Maliev.MessagingContracts.Contracts.Shared;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace Maliev.Intranet.Tests.Bff.Consumers;

public sealed class ProjectStatusChangedAlertConsumerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("intranet_paid_alert_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Consume_WhenProjectIsPaid_EnrichesAlertWithProjectDetail()
    {
        var projectId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<IntranetDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var db = new IntranetDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var hub = CreateHub();
        var projectClient = CreateProjectClient(projectId);
        var consumer = new ProjectStatusChangedAlertConsumer(
            db,
            hub.Object,
            projectClient,
            NullLogger<ProjectStatusChangedAlertConsumer>.Instance);

        await consumer.Consume(CreateContext(projectId, "PRJ-2026-0099").Object);

        var alert = Assert.Single(await db.AlertNotifications.ToListAsync());
        Assert.Equal("ProjectPaid", alert.Type);
        Assert.Equal(projectId, alert.ProjectId);
        Assert.Equal("PRJ-2026-0099", alert.ProjectNumber);
        Assert.Equal("Axion Robotics", alert.CustomerName);
        Assert.Equal(2, alert.PartCount);
        Assert.Equal("CNC, FDM", alert.ProcessTypes);

        hub.ClientProxy.Verify(
            proxy => proxy.SendCoreAsync(
                "ReceiveAlert",
                It.Is<object?[]>(args => IsExpectedPaidProjectSummary(args)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ProjectServiceClient CreateProjectClient(Guid projectId)
    {
        var payload = JsonSerializer.Serialize(new
        {
            id = projectId,
            projectNumber = "PRJ-2026-0099",
            customerId = Guid.NewGuid(),
            customerName = "Axion Robotics",
            title = "Paid Make Studio project",
            status = "Paid",
            currency = "THB",
            createdAt = DateTime.UtcNow,
            parts = new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    fileId = Guid.NewGuid(),
                    fileName = "cnc-bracket.step",
                    processType = "CNC",
                    quantity = 1,
                    status = "Confirmed"
                },
                new
                {
                    id = Guid.NewGuid(),
                    fileId = Guid.NewGuid(),
                    fileName = "printed-cover.stl",
                    processType = "FDM",
                    quantity = 4,
                    status = "Confirmed"
                }
            }
        });
        var client = new HttpClient(new MockHttpMessageHandler((request, _) =>
        {
            var expectedPath = $"/project/v1/projects/{projectId:D}";
            return Task.FromResult(request.RequestUri?.AbsolutePath == expectedPath
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }))
        {
            BaseAddress = new Uri("https://project-service.test")
        };

        return new ProjectServiceClient(client);
    }

    private static bool IsExpectedPaidProjectSummary(object?[] args)
    {
        return args.Length == 1 &&
            args[0] is AlertSummaryDto summary &&
            summary.CustomerName == "Axion Robotics" &&
            summary.PartCount == 2 &&
            summary.ProcessTypes == "CNC, FDM";
    }

    private static HubMocks CreateHub()
    {
        var proxy = new Mock<IClientProxy>();
        proxy
            .Setup(item => item.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.SetupGet(item => item.All).Returns(proxy.Object);

        var hub = new Mock<IHubContext<NotificationHub>>();
        hub.SetupGet(item => item.Clients).Returns(clients.Object);
        return new HubMocks(hub, proxy);
    }

    private static Mock<ConsumeContext<ProjectStatusChangedEvent>> CreateContext(Guid projectId, string projectNumber)
    {
        var message = new ProjectStatusChangedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(ProjectStatusChangedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "ProjectService",
            ConsumedBy: [],
            CorrelationId: projectId,
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new ProjectStatusChangedEventPayload(
                ProjectId: projectId,
                ProjectNumber: projectNumber,
                OldStatus: "Quoted",
                NewStatus: "Paid",
                ChangedAt: DateTimeOffset.UtcNow));

        var context = new Mock<ConsumeContext<ProjectStatusChangedEvent>>();
        context.SetupGet(item => item.CancellationToken).Returns(CancellationToken.None);
        context.SetupGet(item => item.Message).Returns(message);
        return context;
    }

    private sealed record HubMocks(
        Mock<IHubContext<NotificationHub>> Hub,
        Mock<IClientProxy> ClientProxy)
    {
        public IHubContext<NotificationHub> Object => Hub.Object;
    }
}

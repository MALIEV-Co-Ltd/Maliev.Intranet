using System.Diagnostics.Metrics;
using System.Net;
using System.Text;

using Maliev.Intranet.Bff;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Consumers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Consumers;

public sealed class DfmAnalysisReadyConsumerTests
{
    private static readonly Guid FileId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public async Task Consume_WhenFileIdIsValid_PublishesToItsOpaqueFileGroup()
    {
        const string storagePath = "projects/project-1/part.stl";
        var meterFactoryMock = new Mock<IMeterFactory>();
        using var meter = new Meter("test");
        meterFactoryMock.Setup(factory => factory.Create(It.IsAny<MeterOptions>())).Returns(meter);
        var hub = CreateHub(out var clients);
        var consumer = new DfmAnalysisReadyConsumer(
            hub.Object,
            CreateUploadHttpClientFactory(storagePath).Object,
            CreateAnalysisStatusService(storagePath).Object,
            new BffMetrics(meterFactoryMock.Object),
            NullLogger<DfmAnalysisReadyConsumer>.Instance);

        await consumer.Consume(CreateConsumeContext(storagePath).Object);

        clients.Verify(
            item => item.Group(NotificationHub.FileGroup(FileId)),
            Times.Once);
    }

    [Fact]
    public async Task Consume_WhenServerDfmReportsArrive_RecordsConsumedServerCpuDecisions()
    {
        const string storagePath = "projects/project-1/part.stl";
        var meterFactoryMock = new Mock<IMeterFactory>();
        using var meter = new Meter("test");
        meterFactoryMock.Setup(factory => factory.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var measurements = new List<Dictionary<string, object?>>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Name == "intranet_dfm_execution_decisions")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var snapshot = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var tag in tags)
            {
                snapshot[tag.Key] = tag.Value;
            }

            measurements.Add(snapshot);
        });
        listener.Start();

        var httpClientFactory = CreateUploadHttpClientFactory(storagePath);
        var analysisStatusService = CreateAnalysisStatusService(storagePath);
        var hub = CreateHub(out _);
        var metrics = new BffMetrics(meterFactoryMock.Object);
        var consumer = new DfmAnalysisReadyConsumer(
            hub.Object,
            httpClientFactory.Object,
            analysisStatusService.Object,
            metrics,
            NullLogger<DfmAnalysisReadyConsumer>.Instance);
        var context = CreateConsumeContext(storagePath);

        await consumer.Consume(context.Object);

        Assert.Contains(measurements, tags =>
            Equals(tags["process_family"], "fdm")
            && Equals(tags["execution_path"], "server_async_event")
            && Equals(tags["decision"], "server_completed")
            && Equals(tags["server_cpu"], "consumed"));
        Assert.Contains(measurements, tags =>
            Equals(tags["process_family"], "cnc")
            && Equals(tags["execution_path"], "server_async_event")
            && Equals(tags["decision"], "server_completed")
            && Equals(tags["server_cpu"], "consumed"));
    }

    private static Mock<IHttpClientFactory> CreateUploadHttpClientFactory(string storagePath)
    {
        var factory = new Mock<IHttpClientFactory>();
        var client = new HttpClient(new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"storagePath":"{{storagePath}}"}""",
                    Encoding.UTF8,
                    "application/json"),
            })))
        {
            BaseAddress = new Uri("https://upload-service.test"),
        };

        factory
            .Setup(item => item.CreateClient("UploadServiceClient.Consumer"))
            .Returns(client);
        return factory;
    }

    private static Mock<IFileAnalysisStatusService> CreateAnalysisStatusService(string storagePath)
    {
        var service = new Mock<IFileAnalysisStatusService>();
        service
            .Setup(item => item.GetStatusAsync(storagePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileAnalysisStatusDto?)null);
        service
            .Setup(item => item.SetProcessingAsync(storagePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        service
            .Setup(item => item.SetDfmReportsAsync(storagePath, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return service;
    }

    private static Mock<IHubContext<NotificationHub>> CreateHub(out Mock<IHubClients> clients)
    {
        var proxy = new Mock<IClientProxy>();
        proxy
            .Setup(item => item.SendCoreAsync(
                "DfmAnalysisReady",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        clients = new Mock<IHubClients>();
        clients.Setup(item => item.Group(It.IsAny<string>())).Returns(proxy.Object);

        var hub = new Mock<IHubContext<NotificationHub>>();
        hub.SetupGet(item => item.Clients).Returns(clients.Object);
        return hub;
    }

    private static Mock<ConsumeContext<DfmAnalysisReadyEvent>> CreateConsumeContext(string storagePath)
    {
        var context = new Mock<ConsumeContext<DfmAnalysisReadyEvent>>();
        context.SetupGet(item => item.CancellationToken).Returns(CancellationToken.None);
        context.SetupGet(item => item.Message).Returns(new DfmAnalysisReadyEvent
        {
            Payload = new DfmAnalysisReadyEventPayload
            {
                FileId = FileId.ToString(),
                StoragePath = storagePath,
                FdmReport = new FdmDfmReportPayload
                {
                    ReportType = "FDM",
                },
                CncReport = new CncDfmReportPayload
                {
                    ReportType = "CNC_MILL",
                },
                OverlayPaths = new Dictionary<string, string>(),
                AnalyzedAt = DateTimeOffset.UtcNow,
            },
        });
        return context;
    }
}

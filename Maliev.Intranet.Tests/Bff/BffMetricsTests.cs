using System.Diagnostics.Metrics;
using Maliev.Intranet.Bff;
using Moq;

namespace Maliev.Intranet.Tests.Bff;

public class BffMetricsTests
{
    [Fact]
    public void Constructor_ShouldCreateMeter()
    {
        var meterFactoryMock = new Mock<IMeterFactory>();
        var meter = new Meter("test");
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var metrics = new BffMetrics(meterFactoryMock.Object);

        Assert.NotNull(metrics);
        meterFactoryMock.Verify(x => x.Create(It.Is<MeterOptions>(o => o.Name == "intranet-portal")), Times.Once);
    }

    [Fact]
    public void RecordSessionStarted_ShouldNotThrow()
    {
        var meterFactoryMock = new Mock<IMeterFactory>();
        var meter = new Meter("test");
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);
        var metrics = new BffMetrics(meterFactoryMock.Object);

        metrics.RecordSessionStarted();
        metrics.RecordSessionEnded();
    }

    [Fact]
    public void RecordServerDfmProxyRequest_EmitsFallbackMetricTags()
    {
        var meterFactoryMock = new Mock<IMeterFactory>();
        using var meter = new Meter("test");
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var measurements = new List<Dictionary<string, object?>>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Name == "intranet_server_dfm_proxy_requests")
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

        var metrics = new BffMetrics(meterFactoryMock.Object);
        metrics.RecordServerDfmProxyRequest("CNC_MILL", "browser_local_miss");

        var tags = Assert.Single(measurements);
        Assert.Equal("cnc", tags["process_family"]);
        Assert.Equal("browser_local_miss", tags["fallback_reason"]);
    }

    [Fact]
    public void RecordBrowserDfmRuntimeStart_EmitsStartMetricTags()
    {
        var meterFactoryMock = new Mock<IMeterFactory>();
        using var meter = new Meter("test");
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var measurements = new List<Dictionary<string, object?>>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Name == "intranet_browser_dfm_runtime_starts")
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

        var metrics = new BffMetrics(meterFactoryMock.Object);
        metrics.RecordBrowserDfmRuntimeStart("CNC_MILL", "local_primary", "primary_interactive");

        var tags = Assert.Single(measurements);
        Assert.Equal("cnc", tags["process_family"]);
        Assert.Equal("local_primary", tags["authority"]);
        Assert.Equal("primary_interactive", tags["execution_mode"]);
    }
}

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
    public void DfmExecutionDecisionMetrics_EmitBrowserSatisfiedAndServerFallbackTags()
    {
        var meterFactoryMock = new Mock<IMeterFactory>();
        using var meter = new Meter("test");
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

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

        var metrics = new BffMetrics(meterFactoryMock.Object);
        metrics.RecordBrowserDfmRuntimeCompletion("CNC_MILL", true, "local_primary", "primary_interactive");
        metrics.RecordServerDfmProxyRequest("CNC_MILL", "browser_local_miss");

        Assert.Equal(2, measurements.Count);

        var browserTags = measurements[0];
        Assert.Equal("cnc", browserTags["process_family"]);
        Assert.Equal("browser_primary", browserTags["execution_path"]);
        Assert.Equal("satisfied", browserTags["decision"]);
        Assert.Equal("local_primary", browserTags["authority"]);
        Assert.Equal("primary_interactive", browserTags["execution_mode"]);

        var serverTags = measurements[1];
        Assert.Equal("cnc", serverTags["process_family"]);
        Assert.Equal("server_fallback", serverTags["execution_path"]);
        Assert.Equal("server_requested", serverTags["decision"]);
        Assert.Equal("browser_local_miss", serverTags["fallback_reason"]);
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

    [Fact]
    public void RecordBrowserDfmRuntimeStart_RecordsLocalInputWorkload()
    {
        var meterFactoryMock = new Mock<IMeterFactory>();
        using var meter = new Meter("test");
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        var measurements = new Dictionary<string, (long Value, Dictionary<string, object?> Tags)>(StringComparer.Ordinal);
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Name is "intranet_browser_dfm_runtime_input_bytes"
                    or "intranet_browser_dfm_runtime_input_triangles")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            var snapshot = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var tag in tags)
            {
                snapshot[tag.Key] = tag.Value;
            }

            measurements[instrument.Name] = (measurement, snapshot);
        });
        listener.Start();

        var metrics = new BffMetrics(meterFactoryMock.Object);
        metrics.RecordBrowserDfmRuntimeStart("CNC_MILL", "local_primary", "primary_interactive", 84, 1);

        Assert.Equal(84, measurements["intranet_browser_dfm_runtime_input_bytes"].Value);
        Assert.Equal("cnc", measurements["intranet_browser_dfm_runtime_input_bytes"].Tags["process_family"]);
        Assert.Equal(1, measurements["intranet_browser_dfm_runtime_input_triangles"].Value);
        Assert.Equal("cnc", measurements["intranet_browser_dfm_runtime_input_triangles"].Tags["process_family"]);
    }
}

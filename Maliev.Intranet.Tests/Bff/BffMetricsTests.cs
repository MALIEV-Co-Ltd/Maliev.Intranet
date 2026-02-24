using System.Diagnostics.Metrics;
using Maliev.Intranet.Bff;
using Moq;

namespace Maliev.Intranet.Tests.Bff;

/// <summary>Tests for BFF metrics functionality.</summary>
public class BffMetricsTests
{
    /// <summary>Verifies that the constructor creates a meter.</summary>
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

    /// <summary>Verifies that recording a session started and ended does not throw an exception.</summary>
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
}

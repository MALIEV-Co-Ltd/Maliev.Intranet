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
}

using Maliev.Intranet.Shared.Constants;

namespace Maliev.Intranet.Tests.Shared;

public class ManufacturingProcessesTests
{
    [Theory]
    [InlineData("f3d3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d", "3D Printing (FDM)")]
    [InlineData("51a3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d", "3D Printing (SLA)")]
    [InlineData("c4c3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d", "CNC Machining")]
    [InlineData("5ee3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d", "Sheet Metal Fabrication")]
    [InlineData("1413d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d", "Injection Molding")]
    [InlineData("00000000-0000-0000-0000-000000000000", "Unknown Process")]
    public void GetName_ShouldReturnCorrectName(string guidStr, string expectedName)
    {
        var guid = Guid.Parse(guidStr);
        var result = ManufacturingProcesses.GetName(guid);
        Assert.Equal(expectedName, result);
    }
}

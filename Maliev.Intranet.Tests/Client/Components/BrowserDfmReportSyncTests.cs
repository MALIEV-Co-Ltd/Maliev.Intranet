using System.Reflection;

using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class BrowserDfmReportSyncTests
{
    [Fact]
    public void HasCurrentReport_WhenTypedBrowserReportExists_ResolvesReportAndClearsStaleServerFailure()
    {
        var report = new DfmReport
        {
            ReportType = "CNC_MILL",
        };
        var part = new PartViewModel
        {
            ProcessCode = "CNC_MILL",
            CncDfmReport = report,
            DfmAnalysisTimedOut = false,
            AnalysisErrorCode = "FILE_MISSING",
            LocalDfmRuntimeRunningProcessCode = "CNC_MILL",
            LocalDfmRuntimeStartedAtUtc = DateTimeOffset.UtcNow,
            LocalDfmRuntimeTerminalProcessCode = "CNC_MILL",
            LocalDfmRuntimeTerminalReason = "worker_completed",
        };

        var hasCurrentReport = InvokeHasCurrentReport(part, "CNC_MILL");

        Assert.True(hasCurrentReport);
        Assert.Same(report, part.DfmReport);
        Assert.False(part.DfmAnalysisTimedOut);
        Assert.Null(part.AnalysisErrorCode);
        Assert.Null(part.LocalDfmRuntimeRunningProcessCode);
        Assert.Null(part.LocalDfmRuntimeStartedAtUtc);
        Assert.Null(part.LocalDfmRuntimeTerminalProcessCode);
        Assert.Null(part.LocalDfmRuntimeTerminalReason);
    }

    private static bool InvokeHasCurrentReport(PartViewModel part, string processCode)
    {
        var syncType = typeof(PartViewModel).Assembly.GetType(
            "Maliev.Intranet.Client.Components.Project.BrowserDfmReportSync",
            throwOnError: true)!;
        var method = syncType.GetMethod(
            "HasCurrentReport",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.NotNull(method);

        return Assert.IsType<bool>(method.Invoke(null, [part, processCode]));
    }
}

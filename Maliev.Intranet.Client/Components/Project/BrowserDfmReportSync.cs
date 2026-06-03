namespace Maliev.Intranet.Client.Components.Project;

internal static class BrowserDfmReportSync
{
    private const int BrowserDfmGracePeriodMs = 2500;
    private const int BrowserDfmPollIntervalMs = 100;

    internal static bool HasCurrentReport(PartViewModel part, string processCode) =>
        part.DfmReport != null
        && !part.DfmAnalysisTimedOut
        && part.AnalysisErrorCode == null
        && string.Equals(part.ProcessCode, processCode, StringComparison.OrdinalIgnoreCase);

    internal static async Task<bool> WaitForCurrentReportAsync(
        PartViewModel part,
        string processCode,
        CancellationToken cancellationToken)
    {
        if (HasCurrentReport(part, processCode))
            return true;

        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(BrowserDfmGracePeriodMs);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(BrowserDfmPollIntervalMs, cancellationToken);
            if (HasCurrentReport(part, processCode))
                return true;
        }

        return false;
    }
}

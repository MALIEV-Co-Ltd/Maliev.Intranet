namespace Maliev.Intranet.Client.Components.Project;

internal static class BrowserDfmReportSync
{
    // Match the viewer worker's default 15s timeout plus a small callback margin before
    // falling back to the server DFM endpoint. This keeps browser DFM primary.
    private const int BrowserDfmGracePeriodMs = 16_000;
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

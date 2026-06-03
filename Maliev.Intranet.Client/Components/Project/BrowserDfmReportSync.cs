namespace Maliev.Intranet.Client.Components.Project;

internal static class BrowserDfmReportSync
{
    // Cover the manifest's desktop worker timeout plus callback/render margin before
    // falling back to the server DFM endpoint. This keeps browser DFM primary.
    private const int BrowserDfmGracePeriodMs = 30_000;
    private const int BrowserDfmPollIntervalMs = 100;

    internal static bool HasCurrentReport(PartViewModel part, string processCode) =>
        part.DfmReport != null
        && !part.DfmAnalysisTimedOut
        && part.AnalysisErrorCode == null
        && ProcessCodeNormalizer.Equals(part.ProcessCode, processCode);

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

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

    internal static bool HasTerminalLocalAttempt(PartViewModel part, string processCode) =>
        !string.IsNullOrWhiteSpace(part.LocalDfmRuntimeTerminalProcessCode)
        && ProcessCodeNormalizer.Equals(part.LocalDfmRuntimeTerminalProcessCode, processCode);

    internal static void MarkTerminalLocalAttempt(
        PartViewModel part,
        string? processCode,
        string? reason)
    {
        var normalizedProcessCode = ProcessCodeNormalizer.Normalize(processCode)
            ?? ProcessCodeNormalizer.Normalize(part.ProcessCode);
        if (string.IsNullOrWhiteSpace(normalizedProcessCode))
            return;

        part.LocalDfmRuntimeTerminalProcessCode = normalizedProcessCode;
        part.LocalDfmRuntimeTerminalReason = string.IsNullOrWhiteSpace(reason)
            ? "local_runtime_unavailable"
            : reason.Trim();
    }

    internal static void ClearTerminalLocalAttempt(PartViewModel part, string? processCode = null)
    {
        if (!string.IsNullOrWhiteSpace(processCode)
            && !ProcessCodeNormalizer.Equals(part.LocalDfmRuntimeTerminalProcessCode, processCode))
        {
            return;
        }

        part.LocalDfmRuntimeTerminalProcessCode = null;
        part.LocalDfmRuntimeTerminalReason = null;
    }

    internal static async Task<bool> WaitForCurrentReportAsync(
        PartViewModel part,
        string processCode,
        CancellationToken cancellationToken)
    {
        if (HasCurrentReport(part, processCode))
            return true;
        if (HasTerminalLocalAttempt(part, processCode))
            return false;

        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(BrowserDfmGracePeriodMs);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(BrowserDfmPollIntervalMs, cancellationToken);
            if (HasTerminalLocalAttempt(part, processCode))
                return false;
            if (HasCurrentReport(part, processCode))
                return true;
        }

        return false;
    }
}

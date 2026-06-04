namespace Maliev.Intranet.Client.Components.Project;

internal static class BrowserDfmReportSync
{
    // Cover the manifest's desktop worker timeout plus callback/render margin before
    // falling back to the server DFM endpoint. This keeps browser DFM primary.
    private const int BrowserDfmGracePeriodMs = 30_000;
    private const int BrowserDfmStartedGracePeriodMs = 30_000;
    private const int BrowserDfmPollIntervalMs = 100;

    internal static bool HasCurrentReport(PartViewModel part, string processCode) =>
        part.DfmReport != null
        && !part.DfmAnalysisTimedOut
        && part.AnalysisErrorCode == null
        && ProcessCodeNormalizer.Equals(part.ProcessCode, processCode);

    internal static bool HasTerminalLocalAttempt(PartViewModel part, string processCode) =>
        !string.IsNullOrWhiteSpace(part.LocalDfmRuntimeTerminalProcessCode)
        && ProcessCodeNormalizer.Equals(part.LocalDfmRuntimeTerminalProcessCode, processCode);

    internal static bool HasActiveLocalAttempt(PartViewModel part, string processCode) =>
        !string.IsNullOrWhiteSpace(part.LocalDfmRuntimeRunningProcessCode)
        && part.LocalDfmRuntimeStartedAtUtc.HasValue
        && ProcessCodeNormalizer.Equals(part.LocalDfmRuntimeRunningProcessCode, processCode);

    internal static void MarkLocalAttemptStarted(
        PartViewModel part,
        string? processCode)
    {
        var normalizedProcessCode = ProcessCodeNormalizer.Normalize(processCode)
            ?? ProcessCodeNormalizer.Normalize(part.ProcessCode);
        if (string.IsNullOrWhiteSpace(normalizedProcessCode))
            return;

        part.LocalDfmRuntimeRunningProcessCode = normalizedProcessCode;
        part.LocalDfmRuntimeStartedAtUtc = DateTimeOffset.UtcNow;
        ClearTerminalLocalAttempt(part, normalizedProcessCode);
    }

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
        ClearActiveLocalAttempt(part, normalizedProcessCode);
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

    internal static void ClearActiveLocalAttempt(PartViewModel part, string? processCode = null)
    {
        if (!string.IsNullOrWhiteSpace(processCode)
            && !ProcessCodeNormalizer.Equals(part.LocalDfmRuntimeRunningProcessCode, processCode))
        {
            return;
        }

        part.LocalDfmRuntimeRunningProcessCode = null;
        part.LocalDfmRuntimeStartedAtUtc = null;
    }

    internal static DateTimeOffset? GetActiveLocalAttemptDeadline(PartViewModel part, string processCode)
    {
        if (!HasActiveLocalAttempt(part, processCode))
            return null;

        return part.LocalDfmRuntimeStartedAtUtc!.Value.AddMilliseconds(BrowserDfmStartedGracePeriodMs);
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
        while (true)
        {
            var activeDeadline = HasActiveLocalAttempt(part, processCode)
                ? GetActiveLocalAttemptDeadline(part, processCode)
                : null;
            var effectiveDeadline = activeDeadline.HasValue && activeDeadline.Value > deadline
                ? activeDeadline.Value
                : deadline;
            if (DateTimeOffset.UtcNow >= effectiveDeadline)
                return false;

            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(BrowserDfmPollIntervalMs, cancellationToken);
            if (HasTerminalLocalAttempt(part, processCode))
                return false;
            if (HasCurrentReport(part, processCode))
                return true;
        }
    }
}

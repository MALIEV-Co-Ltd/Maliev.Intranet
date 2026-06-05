namespace Maliev.Intranet.Client.Components.Project;

/// <summary>Maps backend geometry error codes to user-friendly status text.</summary>
internal static class DfmStatusMessages
{
    internal const string BrowserLocalDfmUnavailable = "BROWSER_LOCAL_DFM_UNAVAILABLE";
    internal const string PersistedDfmReportUnavailable = "DFM_REPORT_UNAVAILABLE";

    internal static string GetFriendlyMessage(string? errorCode) => errorCode switch
    {
        BrowserLocalDfmUnavailable => "Local DFM could not run on this device. Final manufacturability validation will run before quote.",
        "SIZE_LIMIT_EXCEEDED" => "File too large for analysis (max 200 MB).",
        "MULTI_BODY_ERROR" => "Assembly has multiple bodies — please upload one part at a time.",
        "FILE_CORRUPT" => "Could not read file — it may be corrupt or use unsupported features.",
        "GEOMETRY_PROCESS_TIMEOUT" => "Geometry too complex — analysis timed out. Try a simplified file.",
        "GEOMETRY_PHASE2_TIMEOUT" => "Geometry too complex — analysis timed out. Try a simplified file.",
        "GEOMETRY_WORKER_CRASH" => "Analysis failed for this geometry.",
        "DFM_ANALYZER_FAILED" => "DFM analysis could not be completed for this geometry.",
        PersistedDfmReportUnavailable => "DFM analysis completed previously, but the detailed report is no longer available.",
        "FILE_MISSING" => "File is no longer available — please re-upload.",
        "ANALYSIS_STATUS_NOT_FOUND" => "Analysis status is no longer available — please re-upload.",
        "GEOMETRY_NO_RESULT" => "Analysis did not produce a result. Try re-uploading the file.",
        "CLIENT_TIMEOUT" => "Analysis is taking longer than expected. Try re-uploading the file.",
        _ => "Analysis unavailable.",
    };

    internal static string GetStatusText(string? errorCode) => errorCode switch
    {
        BrowserLocalDfmUnavailable => "Local DFM unavailable",
        "SIZE_LIMIT_EXCEEDED" => "Analysis failed: file too large",
        "MULTI_BODY_ERROR" => "Analysis failed: multi-body assembly",
        "FILE_CORRUPT" => "Analysis failed: corrupt file",
        "GEOMETRY_PROCESS_TIMEOUT" => "Analysis timed out",
        "GEOMETRY_PHASE2_TIMEOUT" => "Analysis timed out",
        "GEOMETRY_WORKER_CRASH" => "Analysis failed",
        "DFM_ANALYZER_FAILED" => "DFM analysis failed",
        PersistedDfmReportUnavailable => "DFM report unavailable",
        "FILE_MISSING" => "File missing — re-upload required",
        "ANALYSIS_STATUS_NOT_FOUND" => "Analysis status unavailable",
        "GEOMETRY_NO_RESULT" => "Analysis failed",
        "CLIENT_TIMEOUT" => "Analysis timed out",
        _ => "Analysis failed",
    };
}

using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Client.Components.Project;

/// <summary>Describes a process change made from the bulk table for one part.</summary>
/// <param name="Part">The part being changed.</param>
/// <param name="Process">The selected process, or null when cleared.</param>
public sealed record ProjectPartProcessChange(PartViewModel Part, ProcessDto? Process);

/// <summary>Describes an explicit bulk patch request from the bulk table.</summary>
/// <param name="Parts">The selected part instances that should receive the patch.</param>
/// <param name="Patch">The explicit field patch to apply.</param>
/// <param name="ShowSummary">True when the caller should show an applied/skipped summary.</param>
public sealed record ProjectPartsBulkApplyRequest(
    IReadOnlyCollection<PartViewModel> Parts,
    PartConfigurationBulkPatch Patch,
    bool ShowSummary = true);

/// <summary>Describes the requested action for a part's DFM table badge.</summary>
public enum ProjectPartDfmAction
{
    /// <summary>Open the DFM review surface for an available report.</summary>
    Review,

    /// <summary>Retry DFM analysis after an unavailable or failed state.</summary>
    Retry,
}

/// <summary>Request raised when a DFM badge is activated in the bulk table.</summary>
/// <param name="Part">The part whose DFM badge was activated.</param>
/// <param name="Action">The action inferred from the current DFM state.</param>
public sealed record ProjectPartDfmActionRequest(
    PartViewModel Part,
    ProjectPartDfmAction Action);

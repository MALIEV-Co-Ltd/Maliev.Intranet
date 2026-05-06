using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Client.Components.Project;

/// <summary>Describes a process change made from the bulk table for one part.</summary>
/// <param name="Part">The part being changed.</param>
/// <param name="Process">The selected process, or null when cleared.</param>
public sealed record ProjectPartProcessChange(PartViewModel Part, ProcessDto? Process);

/// <summary>Describes an explicit bulk patch request from the bulk table.</summary>
/// <param name="Parts">The selected part instances that should receive the patch.</param>
/// <param name="Patch">The explicit field patch to apply.</param>
public sealed record ProjectPartsBulkApplyRequest(
    IReadOnlyCollection<PartViewModel> Parts,
    PartConfigurationBulkPatch Patch);

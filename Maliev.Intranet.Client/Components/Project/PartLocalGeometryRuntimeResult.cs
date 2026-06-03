using Maliev.Intranet.Client.Components;

namespace Maliev.Intranet.Client.Components.Project;

/// <summary>
/// Browser geometry runtime completion associated with a project part.
/// </summary>
public sealed class PartLocalGeometryRuntimeResult
{
    /// <summary>The part whose viewer produced the local result.</summary>
    public required PartViewModel Part { get; init; }

    /// <summary>The local browser runtime result.</summary>
    public required LocalGeometryRuntimeResult Result { get; init; }
}

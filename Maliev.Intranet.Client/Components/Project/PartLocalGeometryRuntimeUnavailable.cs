using Maliev.Intranet.Client.Components;

namespace Maliev.Intranet.Client.Components.Project;

/// <summary>
/// Browser geometry runtime unavailable notification associated with a project part.
/// </summary>
public sealed class PartLocalGeometryRuntimeUnavailable
{
    /// <summary>The part whose viewer produced the terminal local runtime notification.</summary>
    public required PartViewModel Part { get; init; }

    /// <summary>The local browser runtime unavailable notification.</summary>
    public required LocalGeometryRuntimeUnavailable Result { get; init; }
}

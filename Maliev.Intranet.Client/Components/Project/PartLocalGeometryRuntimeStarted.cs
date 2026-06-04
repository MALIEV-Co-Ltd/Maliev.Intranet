using Maliev.Intranet.Client.Components;

namespace Maliev.Intranet.Client.Components.Project;

/// <summary>
/// Associates a browser local geometry runtime start notification with the part it belongs to.
/// </summary>
public sealed class PartLocalGeometryRuntimeStarted
{
    /// <summary>The part whose browser runtime attempt started.</summary>
    public required PartViewModel Part { get; init; }

    /// <summary>The local runtime start notification returned by the viewer.</summary>
    public required LocalGeometryRuntimeStarted Result { get; init; }
}

namespace Maliev.Intranet.Client.Components.Project;

/// <summary>
/// Request to rescale a part's 3D file by a millimeter conversion factor and
/// replace the uploaded file with the scaled result, keeping the configuration.
/// </summary>
public sealed class PartRescaleRequest
{
    /// <summary>Initializes a rescale request.</summary>
    public PartRescaleRequest(PartViewModel part, double scaleFactor)
    {
        Part = part;
        ScaleFactor = scaleFactor;
    }

    /// <summary>The part to rescale.</summary>
    public PartViewModel Part { get; }

    /// <summary>Multiplier applied to every vertex coordinate (e.g. 25.4 for inch→mm).</summary>
    public double ScaleFactor { get; }
}

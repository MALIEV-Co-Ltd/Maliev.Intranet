using Microsoft.AspNetCore.Components.Forms;

namespace Maliev.Intranet.Client.Components.Project;

/// <summary>
/// Request to replace a part's 3D file with a new revision while keeping the
/// part's configuration (process, material, finish, tolerance, quantity).
/// </summary>
public sealed class PartReplaceFileRequest
{
    /// <summary>Initializes a replace-file request.</summary>
    public PartReplaceFileRequest(PartViewModel part, IBrowserFile file, string containerId)
    {
        Part = part;
        File = file;
        ContainerId = containerId;
    }

    /// <summary>The part whose file is being replaced.</summary>
    public PartViewModel Part { get; }

    /// <summary>The newly selected browser file.</summary>
    public IBrowserFile File { get; }

    /// <summary>DOM container id holding the file input (for JS file capture).</summary>
    public string ContainerId { get; }
}

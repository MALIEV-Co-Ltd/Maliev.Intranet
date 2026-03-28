using Maliev.Intranet.Client.Services;
using MudBlazor;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Provides file type configuration and icon mapping loaded from appsettings.json.
/// </summary>
public sealed class FileTypesSettings
{
    private readonly Dictionary<string, string> _iconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".stl"] = Icons.Material.Outlined.ViewInAr,
        [".step"] = Icons.Material.Outlined.ViewInAr,
        [".stp"] = Icons.Material.Outlined.ViewInAr,
        [".3mf"] = Icons.Material.Outlined.ViewInAr,
        [".obj"] = Icons.Material.Outlined.ViewInAr,
        [".igs"] = Icons.Material.Outlined.ViewInAr,
        [".iges"] = Icons.Material.Outlined.ViewInAr,
        [".blend"] = Icons.Material.Outlined.ViewInAr,
        [".fbx"] = Icons.Material.Outlined.ViewInAr,
        [".gltf"] = Icons.Material.Outlined.ViewInAr,
        [".glb"] = Icons.Material.Outlined.ViewInAr,
        [".pdf"] = Icons.Material.Outlined.PictureAsPdf,
        [".dxf"] = Icons.Material.Outlined.Architecture,
        [".dwg"] = Icons.Material.Outlined.Architecture,
        [".png"] = Icons.Material.Outlined.Image,
        [".jpg"] = Icons.Material.Outlined.Image,
        [".jpeg"] = Icons.Material.Outlined.Image,
        [".tiff"] = Icons.Material.Outlined.Image,
        [".bmp"] = Icons.Material.Outlined.Image,
        [".webp"] = Icons.Material.Outlined.Image,
        [".doc"] = Icons.Material.Outlined.Description,
        [".docx"] = Icons.Material.Outlined.Description,
        [".xls"] = Icons.Material.Outlined.TableChart,
        [".xlsx"] = Icons.Material.Outlined.TableChart,
        [".zip"] = Icons.Material.Outlined.FolderZip,
        [".rar"] = Icons.Material.Outlined.FolderZip,
        [".7z"] = Icons.Material.Outlined.FolderZip,
    };

    /// <summary>3D file extensions (STL, STEP, 3MF, OBJ, etc.).</summary>
    public required HashSet<string> ThreeDExtensions { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Technical document extensions (PDF, DXF, DWG).</summary>
    public required HashSet<string> DocumentExtensions { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Image file extensions.</summary>
    public required HashSet<string> ImageExtensions { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Office document extensions (Word, Excel).</summary>
    public required HashSet<string> OfficeExtensions { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Archive file extensions.</summary>
    public required HashSet<string> ArchiveExtensions { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>All supported upload extensions combined.</summary>
    public HashSet<string> AllUploadExtensions => new(
        ThreeDExtensions
            .Concat(DocumentExtensions)
            .Concat(ImageExtensions)
            .Concat(OfficeExtensions)
            .Concat(ArchiveExtensions),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the appropriate MudBlazor icon for a given file extension.
    /// </summary>
    /// <param name="extension">File extension including the leading dot (e.g. ".stl").</param>
    /// <returns>MudBlazor icon string.</returns>
    public string GetIcon(string extension) =>
        _iconMap.TryGetValue(extension, out var icon) ? icon : Icons.Material.Outlined.InsertDriveFile;

    /// <summary>
    /// Returns true if the extension represents a 3D file format.
    /// </summary>
    public bool Is3DFile(string extension) => ThreeDExtensions.Contains(extension);
}

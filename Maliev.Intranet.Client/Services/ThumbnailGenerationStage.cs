namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Stages of client-side thumbnail generation for UI progress reporting.
/// </summary>
public enum ThumbnailGenerationStage
{
    /// <summary>Queued for generation, not yet started.</summary>
    Queued,

    /// <summary>Downloading mesh file from signed GCS URL.</summary>
    Downloading,

    /// <summary>Parsing mesh format (STL/OBJ/GLB/3MF).</summary>
    LoadingMesh,

    /// <summary>Centering and scaling assembly to fit view cube.</summary>
    Normalizing,

    /// <summary>Rendering 8 camera views to RenderTargetTextures.</summary>
    Rendering,

    /// <summary>Encoding pixel buffers to JPEG.</summary>
    Encoding,

    /// <summary>Generation complete, thumbnails ready.</summary>
    Complete,

    /// <summary>Falling back to server-side generation.</summary>
    Fallback,

    /// <summary>Generation failed unrecoverably.</summary>
    Failed
}
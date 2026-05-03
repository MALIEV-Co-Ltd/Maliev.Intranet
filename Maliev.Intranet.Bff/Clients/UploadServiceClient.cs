using System.Net.Http.Headers;
using System.Net.Http.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Upload microservice.
/// </summary>
public class UploadServiceClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates a client using a typed HttpClient (used in HTTP pipeline contexts).
    /// </summary>
    public UploadServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Creates a client from a named HttpClient via IHttpClientFactory (used in background
    /// contexts such as MassTransit consumers where there is no HttpContext).
    /// </summary>
    public UploadServiceClient(string clientName, IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient(clientName);
    }

    /// <summary>
    /// Uploads a file to the central upload service.
    /// Overwrite is enabled by default to handle re-uploads of the same file within a project.
    /// </summary>
    public async Task<BffUploadResponse?> UploadFileAsync(string fileName, Stream content, string contentType, string path, bool overwrite = true, CancellationToken ct = default)
    {
        var totalSize = GetStreamLength(content);
        var session = await InitiateResumableUploadAsync(fileName, contentType, totalSize, path, overwrite, ct);
        if (session == null || string.IsNullOrWhiteSpace(session.SessionUri))
        {
            return null;
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        using var gcsClient = new HttpClient();
        using var uploadContent = new StreamContent(content);
        uploadContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        uploadContent.Headers.ContentLength = totalSize;
        uploadContent.Headers.ContentRange = new ContentRangeHeaderValue(0, totalSize - 1, totalSize);

        var uploadResponse = await gcsClient.PutAsync(session.SessionUri, uploadContent, ct);
        if (!uploadResponse.IsSuccessStatusCode)
        {
            return null;
        }

        return await CompleteResumableUploadAsync(session.UploadId, ct);
    }

    /// <summary>
    /// Initiates a resumable upload session through UploadService.
    /// </summary>
    public async Task<BffResumableUploadSessionResponse?> InitiateResumableUploadAsync(
        string fileName,
        string contentType,
        long fileSize,
        string path,
        bool overwrite = true,
        CancellationToken ct = default)
    {
        var initiateRequest = new InitiateResumableUploadRequest(
            Path: path,
            FileName: fileName,
            ServiceName: "Intranet",
            ContentType: contentType,
            TotalSize: fileSize,
            Overwrite: overwrite);

        var initiateResponse = await _httpClient.PostAsJsonAsync("/upload/v1/uploads/resumable", initiateRequest, ct);
        if (!initiateResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var session = await initiateResponse.Content.ReadFromJsonAsync<InitiateResumableUploadResponse>(cancellationToken: ct);
        if (session == null || string.IsNullOrWhiteSpace(session.SessionUri))
        {
            return null;
        }

        return new BffResumableUploadSessionResponse
        {
            UploadId = session.UploadId,
            SessionUri = session.SessionUri,
            StoragePath = path,
            FileName = fileName,
            FileSize = fileSize,
            ExpiresAt = session.ExpiresAt
        };
    }

    /// <summary>
    /// Completes a resumable upload after the browser has sent bytes to GCS.
    /// </summary>
    public async Task<BffUploadResponse?> CompleteResumableUploadAsync(string uploadId, CancellationToken ct = default)
    {
        var completeResponse = await _httpClient.PostAsJsonAsync(
            $"/upload/v1/uploads/resumable/{uploadId}/complete",
            new { },
            ct);

        if (!completeResponse.IsSuccessStatusCode)
        {
            return null;
        }

        return await completeResponse.Content.ReadFromJsonAsync<BffUploadResponse>(cancellationToken: ct);
    }

    /// <summary>
    /// Proxies a raw resumable upload body to UploadService for clients that cannot reach GCS directly.
    /// </summary>
    public async Task<HttpResponseMessage> ResumeResumableUploadAsync(
        string uploadId,
        Stream content,
        string? contentType,
        long? contentLength,
        string contentRange,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/upload/v1/uploads/resumable/{uploadId}");
        var uploadContent = new StreamContent(content);

        if (!string.IsNullOrWhiteSpace(contentType) &&
            MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
        {
            uploadContent.Headers.ContentType = mediaType;
        }

        if (contentLength.HasValue)
        {
            uploadContent.Headers.ContentLength = contentLength.Value;
        }

        uploadContent.Headers.ContentRange = ContentRangeHeaderValue.Parse(contentRange);
        request.Content = uploadContent;

        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private static long GetStreamLength(Stream content)
    {
        if (!content.CanSeek)
        {
            throw new InvalidOperationException("Direct GCS uploads require a seekable stream so Content-Length and Content-Range can be set.");
        }

        return content.Length;
    }

    /// <summary>
    /// Gets a temporary signed download URL for a file using its UUID uploadId.
    /// </summary>
    public async Task<string?> GetDownloadUrlAsync(string fileReference, CancellationToken ct = default)
    {
        var request = new { ExpirationMinutes = 60 };
        var response = await _httpClient.PostAsJsonAsync($"/upload/v1/files/{fileReference}/signed-url", request, ct);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            if (result.TryGetProperty("signedUrl", out var urlProp))
            {
                return urlProp.GetString();
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the current authoritative GCS storage path for a file by its upload ID.
    /// Returns null if the file is not found in UploadService (404).
    /// </summary>
    public async Task<string?> GetStoragePathAsync(string uploadId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/upload/v1/files/{uploadId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
            return null;
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
        if (result.TryGetProperty("storagePath", out var pathProp))
            return pathProp.GetString();
        return null;
    }

    /// <summary>
    /// Gets a temporary signed download URL for a file using its GCS storage path.
    /// Used by background consumers where there is no HttpContext for user-identity forwarding.
    /// </summary>
    public async Task<string?> GetDownloadUrlByPathAsync(string storagePath, CancellationToken ct = default, int expirationMinutes = 60)
    {
        var request = new { StoragePath = storagePath, ExpirationMinutes = expirationMinutes };
        var response = await _httpClient.PostAsJsonAsync("/upload/v1/files/by-path/signed-url", request, ct);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            if (result.TryGetProperty("signedUrl", out var urlProp))
            {
                return urlProp.GetString();
            }
        }
        return null;
    }

    /// <summary>
    /// Lists uploaded files.
    /// </summary>
    public async Task<PagedResponse<Model3DDto>?> GetFilesAsync(int page = 1, int pageSize = 20, string? filter = null, CancellationToken ct = default)
    {
        var url = $"/upload/v1/files?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(filter))
        {
            url += $"&filter={filter}";
        }

        var response = await _httpClient.GetAsync(url, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<PagedResponse<Model3DDto>>(cancellationToken: ct);
        }
        return null;
    }

    /// <summary>
    /// Gets file metadata by ID.
    /// </summary>
    public async Task<Model3DDto?> GetFileByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/upload/v1/files/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Model3DDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Deletes a file.
    /// </summary>
    public async Task DeleteFileAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"/upload/v1/files/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Migrates all files for a project from the temp bucket to the customer bucket.
    /// Files are copied from <c>projects/{projectId}/...</c> to
    /// <c>customers/{customerId}/projects/{projectId}/...</c>.
    /// </summary>
    /// <param name="projectId">The project GUID whose files should be migrated.</param>
    /// <param name="customerId">The target customer GUID.</param>
    /// <param name="dryRun">If true, only reports what would be migrated without making changes.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<MigrateProjectResponse?> MigrateProjectAsync(Guid projectId, Guid customerId, bool dryRun = false, CancellationToken ct = default)
    {
        var url = $"/upload/v1/admin/migrate-project/{projectId}?customerId={customerId}&dryRun={dryRun.ToString().ToLower()}";
        var response = await _httpClient.PostAsync(url, null, ct);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MigrateProjectResponse>(cancellationToken: ct);
        }

        return null;
    }

    /// <summary>
    /// Copies a GCS object from one storage path to another (cross-bucket supported).
    /// Used to migrate derived artifacts (e.g. _viewer.glb) alongside the original file.
    /// </summary>
    /// <param name="sourcePath">The source storage path.</param>
    /// <param name="destinationPath">The destination storage path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True on success; false on any failure (including 404 NotFound).</returns>
    public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath, CancellationToken ct = default)
    {
        var url = $"/upload/v1/admin/copy-file?sourcePath={Uri.EscapeDataString(sourcePath)}&destinationPath={Uri.EscapeDataString(destinationPath)}";
        var response = await _httpClient.PostAsync(url, null, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Copies a GCS object and creates independent UploadService metadata for the copied file.
    /// </summary>
    public async Task<CopyFileWithMetadataResponse?> CopyFileWithMetadataAsync(
        string sourcePath,
        string destinationPath,
        string fileName,
        string serviceName = "Intranet",
        Dictionary<string, string>? metadata = null,
        CancellationToken ct = default)
    {
        var request = new
        {
            sourcePath,
            destinationPath,
            fileName,
            serviceName,
            metadata
        };

        var response = await _httpClient.PostAsJsonAsync("/upload/v1/admin/copy-file-with-metadata", request, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CopyFileWithMetadataResponse>(cancellationToken: ct);
    }
}

/// <summary>
/// Response from UploadService when a file has been copied with independent metadata.
/// </summary>
public class CopyFileWithMetadataResponse
{
    /// <summary>Gets or sets the copied file ID.</summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>Gets or sets the copied upload ID.</summary>
    public string UploadId { get; set; } = string.Empty;

    /// <summary>Gets or sets the copied storage path.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the copied file name.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the copied file size in bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Gets or sets the copied content type.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the copy timestamp.</summary>
    public DateTime UploadedAt { get; set; }
}

/// <summary>
/// Response from the single-project migration endpoint.
/// </summary>
public class MigrateProjectResponse
{
    /// <summary>Gets or sets whether this was a dry run with no actual changes.</summary>
    public bool DryRun { get; set; }

    /// <summary>Gets or sets the total number of files evaluated for migration.</summary>
    public int TotalEvaluated { get; set; }

    /// <summary>Gets or sets the total number of files successfully migrated.</summary>
    public int TotalMigrated { get; set; }

    /// <summary>Gets or sets the list of individual file migration results.</summary>
    public List<MigratedFileEntry> MigratedFiles { get; set; } = [];

    /// <summary>Gets or sets any errors encountered during migration.</summary>
    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// Represents a single migrated file with old and new paths.
/// </summary>
public class MigratedFileEntry
{
    /// <summary>Gets or sets the unique identifier of the migrated file.</summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>Gets or sets the original storage path before migration.</summary>
    public string OldPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the new storage path after migration.</summary>
    public string NewPath { get; set; } = string.Empty;
}

internal sealed record InitiateResumableUploadRequest(
    string Path,
    string FileName,
    string ServiceName,
    string ContentType,
    long TotalSize,
    bool Overwrite);

internal sealed record InitiateResumableUploadResponse(
    string UploadId,
    string SessionUri,
    DateTime ExpiresAt,
    long TotalSize);

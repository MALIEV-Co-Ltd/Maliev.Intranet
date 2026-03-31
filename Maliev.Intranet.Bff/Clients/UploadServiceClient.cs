using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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
        using var requestContent = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        requestContent.Add(fileContent, "File", fileName);
        requestContent.Add(new StringContent(path), "Path");
        requestContent.Add(new StringContent(overwrite.ToString().ToLower()), "Overwrite");

        var response = await _httpClient.PostAsync("/upload/v1/uploads", requestContent, ct);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BffUploadResponse>(cancellationToken: ct);
        }

        return null;
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

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
}

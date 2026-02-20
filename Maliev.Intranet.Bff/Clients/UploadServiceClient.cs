using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Upload microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class UploadServiceClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    /// <summary>
    /// Uploads a file to the central upload service.
    /// </summary>
    public async Task<BffUploadResponse?> UploadFileAsync(string fileName, Stream content, string contentType, string path, CancellationToken ct = default)
    {
        using var requestContent = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        requestContent.Add(fileContent, "File", fileName);
        requestContent.Add(new StringContent(path), "Path");

        var response = await _httpClient.PostAsync("/upload/v1/uploads", requestContent, ct);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BffUploadResponse>(cancellationToken: ct);
        }

        return null;
    }

    /// <summary>
    /// Gets a temporary signed download URL for a file.
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

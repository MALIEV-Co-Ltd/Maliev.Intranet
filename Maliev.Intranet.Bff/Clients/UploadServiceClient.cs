using Maliev.Intranet.Shared;
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
    /// <param name="fileName">Name of the file.</param>
    /// <param name="content">File content stream.</param>
    /// <param name="contentType">MIME type of the file.</param>
    /// <param name="path">Target storage path.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Upload response metadata.</returns>
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
            return await response.Content.ReadFromJsonAsync<BffUploadResponse>(ct);
        }

        return null;
    }

    /// <summary>
    /// Gets a temporary signed download URL for a file.
    /// </summary>
    public async Task<string?> GetDownloadUrlAsync(string fileReference, CancellationToken ct = default)
    {
        // The UploadService uses POST /upload/v1/files/{uploadId}/signed-url
        var request = new { ExpirationMinutes = 60 };
        var response = await _httpClient.PostAsJsonAsync($"/upload/v1/files/{fileReference}/signed-url", request, ct);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
            if (result.TryGetProperty("signedUrl", out var urlProp))
            {
                return urlProp.GetString();
            }
        }
        return null;
    }
}

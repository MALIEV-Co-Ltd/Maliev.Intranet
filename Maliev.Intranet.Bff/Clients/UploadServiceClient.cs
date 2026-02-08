using Maliev.Intranet.Shared;
using System.Net.Http.Headers;

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
}

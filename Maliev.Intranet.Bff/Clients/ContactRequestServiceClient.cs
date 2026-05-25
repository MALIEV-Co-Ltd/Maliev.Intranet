using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client interface for website contact request management through ContactService.
/// </summary>
public interface IContactRequestServiceClient
{
    /// <summary>Gets a paged contact request list.</summary>
    Task<PagedResponse<ContactRequestDto>> GetContactRequestsAsync(int page, int pageSize, ContactRequestStatus? status, string? email, CancellationToken ct = default);

    /// <summary>Gets a single contact request.</summary>
    Task<ContactRequestDto?> GetContactRequestAsync(int id, CancellationToken ct = default);

    /// <summary>Updates contact request status and priority.</summary>
    Task<ContactRequestDto?> UpdateContactStatusAsync(int id, ContactRequestStatusUpdateRequest request, CancellationToken ct = default);

    /// <summary>Downloads a contact request file.</summary>
    Task<ContactRequestFileDownload?> DownloadContactFileAsync(int id, int fileId, CancellationToken ct = default);
}

/// <summary>
/// Downloaded contact request file content.
/// </summary>
public sealed record ContactRequestFileDownload(byte[] Content, string ContentType, string FileName);

/// <summary>
/// ContactService-backed client for website contact requests.
/// </summary>
/// <param name="httpClient">The configured ContactService HTTP client.</param>
public sealed class ContactRequestServiceClient(HttpClient httpClient) : IContactRequestServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc />
    public async Task<PagedResponse<ContactRequestDto>> GetContactRequestsAsync(
        int page,
        int pageSize,
        ContactRequestStatus? status,
        string? email,
        CancellationToken ct = default)
    {
        var url = $"/contact/v1/contacts?page={Math.Max(1, page)}&pageSize={NormalizePageSize(pageSize)}";
        if (status.HasValue)
        {
            url += $"&status={(int)status.Value}";
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            url += $"&email={Uri.EscapeDataString(email.Trim())}";
        }

        var contacts = await httpClient.GetFromJsonAsync<List<ContactRequestDto>>(url, JsonOptions, ct) ?? [];
        foreach (var contact in contacts)
        {
            Normalize(contact);
        }

        return new PagedResponse<ContactRequestDto>
        {
            Data = contacts,
            Meta = new PaginationMeta
            {
                CurrentPage = Math.Max(1, page),
                PageSize = NormalizePageSize(pageSize),
                TotalCount = contacts.Count,
                TotalItems = contacts.Count,
                TotalPages = contacts.Count == 0 ? 0 : 1
            }
        };
    }

    /// <inheritdoc />
    public async Task<ContactRequestDto?> GetContactRequestAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/contact/v1/contacts/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var contact = await response.Content.ReadFromJsonAsync<ContactRequestDto>(JsonOptions, ct);
        return contact is null ? null : Normalize(contact);
    }

    /// <inheritdoc />
    public async Task<ContactRequestDto?> UpdateContactStatusAsync(int id, ContactRequestStatusUpdateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"/contact/v1/contacts/{id}/status",
            new
            {
                status = (int)request.Status,
                priority = request.Priority.HasValue ? (int)request.Priority.Value : (int?)null
            },
            JsonOptions,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var contact = await response.Content.ReadFromJsonAsync<ContactRequestDto>(JsonOptions, ct);
        return contact is null ? null : Normalize(contact);
    }

    /// <inheritdoc />
    public async Task<ContactRequestFileDownload?> DownloadContactFileAsync(int id, int fileId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/contact/v1/contacts/{id}/files/{fileId}/download", HttpCompletionOption.ResponseHeadersRead, ct);
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsByteArrayAsync(ct);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var fileName = ResolveFileName(response.Content.Headers.ContentDisposition) ?? $"contact-{id}-file-{fileId}";
            return new ContactRequestFileDownload(content, contentType, fileName);
        }
    }

    private static ContactRequestDto Normalize(ContactRequestDto contact)
    {
        contact.PublicReference = string.IsNullOrWhiteSpace(contact.PublicReference)
            ? FormatPublicReference(contact.Id)
            : contact.PublicReference;
        return contact;
    }

    private static string FormatPublicReference(int id) => FormattableString.Invariant($"MLV-C-{id:000000}");

    private static int NormalizePageSize(int pageSize) => pageSize switch
    {
        <= 10 => 10,
        <= 20 => 20,
        <= 50 => 50,
        _ => 100
    };

    private static string? ResolveFileName(ContentDispositionHeaderValue? disposition)
    {
        return disposition?.FileNameStar?.Trim('"')
            ?? disposition?.FileName?.Trim('"');
    }
}

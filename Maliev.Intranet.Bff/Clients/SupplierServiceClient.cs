using Maliev.Intranet.Shared;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Supplier microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class SupplierServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of suppliers.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="status">Optional supplier lifecycle status filter.</param>
    /// <param name="capability">Optional capability filter.</param>
    /// <param name="search">Optional supplier search term.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing supplier summaries.</returns>
    public async Task<PagedResponse<SupplierSummaryDto>?> GetSuppliersAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? capability = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var query = new List<string>
        {
            $"page={page.ToString(CultureInfo.InvariantCulture)}",
            $"pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}"
        };

        AddQueryParameter(query, "status", status);
        AddQueryParameter(query, "capability", capability);
        AddQueryParameter(query, "search", search);

        var response = await httpClient.GetAsync($"/supplier/v1/suppliers?{string.Join("&", query)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var downstream = await response.Content.ReadFromJsonAsync<SupplierListResponse>(cancellationToken: ct);
        return downstream?.ToPagedResponse();
    }

    /// <summary>
    /// Finds active suppliers that can supply a material based on process and category capabilities.
    /// </summary>
    /// <param name="capabilities">Capability names inferred from the material.</param>
    /// <param name="fallbackSearch">Fallback supplier search text when capability matches are empty.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Supplier summaries that match the material.</returns>
    public async Task<List<SupplierSummaryDto>> FindMaterialSuppliersAsync(
        IEnumerable<string> capabilities,
        string? fallbackSearch = null,
        CancellationToken ct = default)
    {
        var matches = new Dictionary<Guid, SupplierSummaryDto>();
        foreach (var capability in capabilities.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).Take(6))
        {
            var response = await GetSuppliersAsync(1, 50, "Active", capability, null, ct);
            if (response is null)
            {
                continue;
            }

            foreach (var supplier in response.Data)
            {
                if (!matches.TryGetValue(supplier.Id, out var existing))
                {
                    supplier.Capabilities = [capability];
                    supplier.MatchReason = $"Matches {capability}";
                    matches[supplier.Id] = supplier;
                    continue;
                }

                if (!existing.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase))
                {
                    existing.Capabilities.Add(capability);
                    existing.MatchReason = $"Matches {string.Join(", ", existing.Capabilities)}";
                }
            }
        }

        if (matches.Count == 0 && !string.IsNullOrWhiteSpace(fallbackSearch))
        {
            var response = await GetSuppliersAsync(1, 20, "Active", null, fallbackSearch, ct);
            if (response is not null)
            {
                foreach (var supplier in response.Data)
                {
                    supplier.MatchReason = "Matches material search";
                    matches.TryAdd(supplier.Id, supplier);
                }
            }
        }

        return matches.Values
            .OrderByDescending(supplier => supplier.Capabilities.Count)
            .ThenBy(supplier => supplier.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Retrieves detailed information for a single supplier by ID.
    /// </summary>
    /// <param name="id">The supplier ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The supplier detail DTO.</returns>
    public async Task<SupplierDetailDto?> GetSupplierByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/supplier/v1/suppliers/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var downstream = await response.Content.ReadFromJsonAsync<SupplierDetailResponse>(cancellationToken: ct);
        return downstream?.ToDto();
    }

    /// <summary>
    /// Creates a new supplier.
    /// </summary>
    public async Task<HttpResponseMessage> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken ct = default)
    {
        var downstreamRequest = new DownstreamCreateSupplierRequest(
            request.Name,
            request.TaxId,
            request.Address ?? string.Empty,
            request.City,
            request.Country,
            request.PostalCode,
            null,
            request.Capabilities,
            new DownstreamCreateContactRequest(
                string.IsNullOrWhiteSpace(request.ContactPerson) ? request.Name : request.ContactPerson,
                request.Email,
                "Primary",
                request.Phone ?? string.Empty,
                true));

        return await httpClient.PostAsJsonAsync("/supplier/v1/suppliers", downstreamRequest, ct);
    }

    /// <summary>
    /// Updates an existing supplier.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateSupplierAsync(Guid id, UpdateSupplierRequest request, CancellationToken ct = default)
    {
        var downstreamRequest = new DownstreamUpdateSupplierRequest(
            string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            string.IsNullOrWhiteSpace(request.Country) ? null : request.Country.Trim(),
            string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim(),
            null,
            request.Capabilities,
            request.RowVersion ?? string.Empty);

        return await httpClient.PutAsJsonAsync($"/supplier/v1/suppliers/{id}", downstreamRequest, ct);
    }

    /// <summary>
    /// Updates a supplier lifecycle status.
    /// </summary>
    /// <param name="id">The supplier identifier.</param>
    /// <param name="request">The status update request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The downstream SupplierService response.</returns>
    public async Task<HttpResponseMessage> UpdateSupplierStatusAsync(Guid id, UpdateSupplierStatusRequest request, CancellationToken ct = default)
    {
        var downstreamRequest = new DownstreamUpdateSupplierStatusRequest(
            request.Status.Trim(),
            string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            request.RowVersion);

        return await httpClient.PatchAsJsonAsync($"/supplier/v1/suppliers/{id}/status", downstreamRequest, ct);
    }

    /// <summary>
    /// Deactivates a supplier.
    /// </summary>
    /// <param name="id">The supplier identifier.</param>
    /// <param name="rowVersion">The current supplier row version.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The downstream SupplierService response.</returns>
    public async Task<HttpResponseMessage> DeactivateSupplierAsync(Guid id, string rowVersion, CancellationToken ct = default)
    {
        return await UpdateSupplierStatusAsync(id, new UpdateSupplierStatusRequest
        {
            Status = "Inactive",
            Reason = "Deactivated from Intranet.",
            RowVersion = rowVersion
        }, ct);
    }

    /// <summary>
    /// Adds supplier document metadata after an optional UploadService file upload.
    /// </summary>
    public async Task<HttpResponseMessage> AddSupplierDocumentAsync(Guid id, CreateSupplierDocumentRequest request, CancellationToken ct = default)
    {
        var downstreamRequest = new DownstreamCreateSupplierDocumentRequest(
            request.DocumentType,
            request.DocumentName,
            request.IssueDate,
            request.ExpirationDate,
            request.ExternalFileRef,
            request.Notes);

        return await httpClient.PostAsJsonAsync($"/supplier/v1/suppliers/{id}/certifications", downstreamRequest, ct);
    }

    /// <summary>
    /// Deletes supplier document metadata.
    /// </summary>
    public async Task<HttpResponseMessage> DeleteSupplierDocumentAsync(Guid supplierId, Guid documentId, CancellationToken ct = default)
    {
        return await httpClient.DeleteAsync($"/supplier/v1/suppliers/{supplierId}/certifications/{documentId}", ct);
    }

    private static void AddQueryParameter(List<string> query, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value.Trim())}");
    }

    private sealed record SupplierListResponse(
        IReadOnlyList<SupplierResponse> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages)
    {
        public PagedResponse<SupplierSummaryDto> ToPagedResponse()
        {
            return new PagedResponse<SupplierSummaryDto>
            {
                Data = Items.Select(item => item.ToSummaryDto()).ToList(),
                Meta = new PaginationMeta
                {
                    CurrentPage = Page,
                    PageSize = PageSize,
                    TotalCount = TotalCount,
                    TotalItems = TotalCount,
                    TotalPages = TotalPages
                }
            };
        }
    }

    private sealed record SupplierResponse(
        Guid Id,
        string CompanyName,
        string? TaxId,
        string? Address,
        string? City,
        string? Country,
        string? PostalCode,
        string? Status,
        DateTime CreatedAt)
    {
        public SupplierSummaryDto ToSummaryDto()
        {
            return new SupplierSummaryDto
            {
                Id = Id,
                Name = CompanyName,
                TaxId = TaxId,
                City = City,
                Country = Country,
                Status = Status ?? string.Empty,
                Email = string.Empty,
                Rating = 0m
            };
        }
    }

    private sealed record SupplierDetailResponse(
        Guid Id,
        string CompanyName,
        string? TaxId,
        string? Address,
        string? City,
        string? Country,
        string? PostalCode,
        string? Status,
        string? OnboardingStage,
        string? RowVersion,
        IReadOnlyList<SupplierContactResponse>? Contacts,
        IReadOnlyList<SupplierDocumentResponse>? Certifications,
        IReadOnlyList<CapabilityResponse>? Capabilities,
        PerformanceSummaryResponse? PerformanceSummary,
        DateTime CreatedAt)
    {
        public SupplierDetailDto ToDto()
        {
            var contact = Contacts?.FirstOrDefault();
            return new SupplierDetailDto
            {
                Id = Id,
                Name = CompanyName,
                TaxId = TaxId,
                Email = contact?.Email ?? string.Empty,
                Phone = contact?.PhoneNumber,
                Country = Country ?? string.Empty,
                Address = Address,
                City = City,
                PostalCode = PostalCode,
                Status = Status ?? string.Empty,
                Rating = PerformanceSummary?.OverallRating ?? 0m,
                ContactPerson = contact?.Name,
                Capabilities = Capabilities?.Where(capability => capability.IsActive).Select(capability => capability.Name).ToList() ?? [],
                RowVersion = RowVersion ?? string.Empty,
                OnboardingStage = OnboardingStage ?? string.Empty,
                Documents = Certifications?.Select(certification => certification.ToDto()).ToList() ?? [],
                CreatedAt = CreatedAt
            };
        }
    }

    private sealed record SupplierContactResponse(
        string Name,
        string? Email,
        [property: JsonPropertyName("phoneNumber")] string? PhoneNumber);

    private sealed record CapabilityResponse(
        string Name,
        bool IsActive);

    private sealed record PerformanceSummaryResponse(decimal? OverallRating);

    private sealed record SupplierDocumentResponse(
        Guid Id,
        string DocumentType,
        string DocumentName,
        DateOnly? IssueDate,
        DateOnly? ExpirationDate,
        string? ExternalFileRef,
        bool IsExpired,
        bool IsExpiringSoon,
        DateTime CreatedAt)
    {
        public SupplierDocumentDto ToDto()
        {
            return new SupplierDocumentDto
            {
                Id = Id,
                DocumentType = DocumentType,
                DocumentName = DocumentName,
                IssueDate = IssueDate,
                ExpirationDate = ExpirationDate,
                ExternalFileRef = ExternalFileRef,
                IsExpired = IsExpired,
                IsExpiringSoon = IsExpiringSoon,
                CreatedAt = CreatedAt
            };
        }
    }

    private sealed record DownstreamCreateSupplierRequest(
        [property: JsonPropertyName("companyName")] string CompanyName,
        [property: JsonPropertyName("taxId")] string TaxId,
        [property: JsonPropertyName("address")] string Address,
        [property: JsonPropertyName("city")] string City,
        [property: JsonPropertyName("country")] string Country,
        [property: JsonPropertyName("postalCode")] string? PostalCode,
        [property: JsonPropertyName("materialCategoryIds")] IEnumerable<Guid>? MaterialCategoryIds,
        [property: JsonPropertyName("capabilities")] IEnumerable<string>? Capabilities,
        [property: JsonPropertyName("primaryContact")] DownstreamCreateContactRequest? PrimaryContact);

    private sealed record DownstreamCreateContactRequest(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("phone")] string Phone,
        [property: JsonPropertyName("isPrimary")] bool IsPrimary);

    private sealed record DownstreamUpdateSupplierRequest(
        [property: JsonPropertyName("companyName")] string? CompanyName,
        [property: JsonPropertyName("address")] string? Address,
        [property: JsonPropertyName("city")] string? City,
        [property: JsonPropertyName("country")] string? Country,
        [property: JsonPropertyName("postalCode")] string? PostalCode,
        [property: JsonPropertyName("materialCategoryIds")] IEnumerable<Guid>? MaterialCategoryIds,
        [property: JsonPropertyName("capabilities")] IEnumerable<string>? Capabilities,
        [property: JsonPropertyName("rowVersion")] string RowVersion);

    private sealed record DownstreamUpdateSupplierStatusRequest(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("reason")] string? Reason,
        [property: JsonPropertyName("rowVersion")] string RowVersion);

    private sealed record DownstreamCreateSupplierDocumentRequest(
        [property: JsonPropertyName("documentType")] string DocumentType,
        [property: JsonPropertyName("documentName")] string DocumentName,
        [property: JsonPropertyName("issueDate")] DateOnly IssueDate,
        [property: JsonPropertyName("expirationDate")] DateOnly? ExpirationDate,
        [property: JsonPropertyName("externalFileRef")] string? ExternalFileRef,
        [property: JsonPropertyName("notes")] string? Notes);
}

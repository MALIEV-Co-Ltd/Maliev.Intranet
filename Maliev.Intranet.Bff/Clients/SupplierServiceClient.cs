using Maliev.Intranet.Shared;
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
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing supplier summaries.</returns>
    public async Task<PagedResponse<SupplierSummaryDto>?> GetSuppliersAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/supplier/v1/suppliers?page={page}&pageSize={pageSize}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var downstream = await response.Content.ReadFromJsonAsync<SupplierListResponse>(cancellationToken: ct);
        return downstream?.ToPagedResponse();
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
        return await httpClient.PostAsJsonAsync("/supplier/v1/suppliers", request, ct);
    }

    /// <summary>
    /// Updates an existing supplier.
    /// </summary>
    public async Task<HttpResponseMessage> UpdateSupplierAsync(Guid id, UpdateSupplierRequest request, CancellationToken ct = default)
    {
        return await httpClient.PutAsJsonAsync($"/supplier/v1/suppliers/{id}", request, ct);
    }

    /// <summary>
    /// Deactivates a supplier.
    /// </summary>
    public async Task<HttpResponseMessage> DeactivateSupplierAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.PatchAsJsonAsync($"/supplier/v1/suppliers/{id}/status", new
        {
            Status = "Inactive",
            Reason = "Deactivated from Intranet.",
            RowVersion = string.Empty
        }, ct);
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
                Status = Status ?? string.Empty,
                Email = string.Empty,
                Rating = 0m
            };
        }
    }

    private sealed record SupplierDetailResponse(
        Guid Id,
        string CompanyName,
        string? Address,
        string? City,
        string? Country,
        string? PostalCode,
        string? Status,
        IReadOnlyList<SupplierContactResponse>? Contacts,
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
                Email = contact?.Email ?? string.Empty,
                Phone = contact?.PhoneNumber,
                Country = Country ?? string.Empty,
                Address = string.Join(", ", new[] { Address, City, PostalCode, Country }.Where(value => !string.IsNullOrWhiteSpace(value))),
                Status = Status ?? string.Empty,
                Rating = PerformanceSummary?.OverallRating ?? 0m,
                ContactPerson = contact?.Name,
                CreatedAt = CreatedAt
            };
        }
    }

    private sealed record SupplierContactResponse(
        string Name,
        string? Email,
        [property: JsonPropertyName("phoneNumber")] string? PhoneNumber);

    private sealed record PerformanceSummaryResponse(decimal? OverallRating);
}

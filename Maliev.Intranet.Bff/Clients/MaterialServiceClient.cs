using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Material microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
public class MaterialServiceClient(HttpClient httpClient)
{
    /// <summary>
    /// Retrieves a paged list of materials.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing material summaries.</returns>
    public async Task<PagedResponse<MaterialSummaryDto>?> GetMaterialsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<MaterialServicePagedResult<MaterialServiceMaterialDto>>($"/material/v1/materials?page={page}&pageSize={pageSize}", ct);

        return response is null
            ? null
            : new PagedResponse<MaterialSummaryDto>
            {
                Data = response.Items.Select(ToSummaryDto),
                Meta = new PaginationMeta
                {
                    CurrentPage = response.Page,
                    PageSize = response.PageSize,
                    TotalItems = response.TotalCount,
                    TotalCount = response.TotalCount,
                    TotalPages = response.TotalPages
                }
            };
    }

    /// <summary>
    /// Retrieves detailed information for a single material by ID.
    /// </summary>
    /// <param name="id">The material ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The material detail DTO.</returns>
    public async Task<MaterialDetailDto?> GetMaterialByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<MaterialServiceMaterialDto>($"/material/v1/materials/{id}", ct);
        return response is null ? null : ToDetailDto(response);
    }

    /// <summary>
    /// Creates a new material.
    /// </summary>
    /// <param name="request">The creation request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created material summary.</returns>
    public async Task<MaterialSummaryDto?> CreateMaterialAsync(CreateMaterialRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/material/v1/materials", request, ct);
        if (response.IsSuccessStatusCode)
        {
            var material = await response.Content.ReadFromJsonAsync<MaterialServiceMaterialDto>(cancellationToken: ct);
            return material is null ? null : ToSummaryDto(material);
        }
        return null;
    }

    /// <summary>
    /// Updates an existing material.
    /// </summary>
    /// <param name="id">The material ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated material detail.</returns>
    public async Task<MaterialDetailDto?> UpdateMaterialAsync(Guid id, UpdateMaterialRequest request, CancellationToken ct = default)
    {
        var current = await httpClient.GetFromJsonAsync<MaterialServiceMaterialDto>($"/material/v1/materials/{id}", ct);
        if (current is null)
        {
            return null;
        }

        var downstreamRequest = new MaterialServiceUpdateMaterialRequest
        {
            Name = FirstNonWhiteSpace(request.Name, current.Name),
            Code = FirstNonWhiteSpace(request.SKU, current.Code),
            Description = request.Description ?? current.Description,
            PricePerUnit = request.UnitPrice ?? current.PricePerUnit,
            StockLevel = request.QuantityOnHand ?? current.StockLevel,
            SupplierId = current.SupplierId,
            ManufacturingProcessIds = current.ManufacturingProcesses.Select(process => process.Id).ToList(),
            ColorIds = current.AvailableColors.Select(color => color.Id).ToList(),
            PostProcessingMethodIds = current.PostProcessingMethods.Select(method => method.Id).ToList(),
            MechanicalProperties = current.MechanicalProperties
                .Select(property => new MaterialServiceMechanicalPropertyRequest
                {
                    MechanicalPropertyId = property.MechanicalPropertyId,
                    Value = property.Value
                })
                .ToList()
        };

        var response = await httpClient.PutAsJsonAsync($"/material/v1/materials/{id}", downstreamRequest, ct);
        if (response.IsSuccessStatusCode)
        {
            var material = await response.Content.ReadFromJsonAsync<MaterialServiceMaterialDto>(cancellationToken: ct);
            return material is null ? null : ToDetailDto(material);
        }
        return null;
    }

    /// <summary>
    /// Deletes a material.
    /// </summary>
    /// <param name="id">The material ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> DeleteMaterialAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/material/v1/materials/{id}", ct);
        return response.IsSuccessStatusCode;
    }

    // ── Manufacturing Catalog ─────────────────────────────────────────────────

    /// <summary>Returns all active manufacturing processes.</summary>
    public Task<List<ProcessDto>?> GetProcessesAsync(CancellationToken ct = default) =>
        httpClient.GetFromJsonAsync<List<ProcessDto>>("/material/v1/manufacturing/processes", ct);

    /// <summary>Returns materials available for the given process code.</summary>
    public Task<List<CatalogMaterialDto>?> GetMaterialsByProcessAsync(string processCode, CancellationToken ct = default) =>
        httpClient.GetFromJsonAsync<List<CatalogMaterialDto>>($"/material/v1/manufacturing/processes/{Uri.EscapeDataString(processCode)}/materials", ct);

    /// <summary>Returns surface finishes available for the given process code.</summary>
    public Task<List<CatalogSurfaceFinishDto>?> GetFinishesByProcessAsync(string processCode, CancellationToken ct = default) =>
        httpClient.GetFromJsonAsync<List<CatalogSurfaceFinishDto>>($"/material/v1/manufacturing/processes/{Uri.EscapeDataString(processCode)}/finishes", ct);

    /// <summary>Returns tolerance classes available for the given process code.</summary>
    public Task<List<CatalogToleranceDto>?> GetTolerancesByProcessAsync(string processCode, CancellationToken ct = default) =>
        httpClient.GetFromJsonAsync<List<CatalogToleranceDto>>($"/material/v1/manufacturing/processes/{Uri.EscapeDataString(processCode)}/tolerances", ct);

    /// <summary>Returns dynamic configuration options for the given process code.</summary>
    public Task<List<ProcessConfigOptionDto>?> GetConfigOptionsByProcessAsync(string processCode, CancellationToken ct = default) =>
        httpClient.GetFromJsonAsync<List<ProcessConfigOptionDto>>($"/material/v1/manufacturing/processes/{Uri.EscapeDataString(processCode)}/config-options", ct);

    /// <summary>Returns surface finishes compatible with a specific material.</summary>
    public Task<List<CatalogSurfaceFinishDto>?> GetFinishesByMaterialAsync(Guid materialId, CancellationToken ct = default) =>
        httpClient.GetFromJsonAsync<List<CatalogSurfaceFinishDto>>($"/material/v1/manufacturing/materials/{materialId}/finishes", ct);

    private static MaterialSummaryDto ToSummaryDto(MaterialServiceMaterialDto material)
    {
        return new MaterialSummaryDto
        {
            Id = material.Id,
            Name = material.Name,
            SKU = material.Code,
            Category = string.Join(", ", material.ManufacturingProcesses.Select(process => process.Name)),
            QuantityOnHand = material.StockLevel,
            ReorderLevel = 0,
            UnitPrice = material.PricePerUnit,
            Status = material.Active ? "Active" : "Inactive",
            Unit = "pcs"
        };
    }

    private static string FirstNonWhiteSpace(string? candidate, string fallback)
        => string.IsNullOrWhiteSpace(candidate) ? fallback : candidate.Trim();

    private static MaterialDetailDto ToDetailDto(MaterialServiceMaterialDto material)
    {
        var manufacturingProcesses = material.ManufacturingProcesses
            .Select(process => process.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        var colors = material.AvailableColors
            .Select(color => new MaterialColorDto
            {
                Id = color.Id,
                Name = color.Name,
                HexCode = color.HexCode
            })
            .ToList();

        return new MaterialDetailDto
        {
            Id = material.Id,
            Name = material.Name,
            SKU = material.Code,
            Category = string.Join(", ", manufacturingProcesses),
            Description = material.Description ?? string.Empty,
            UnitPrice = material.PricePerUnit,
            QuantityOnHand = material.StockLevel,
            ReorderLevel = 0,
            Unit = "pcs",
            Status = material.Active ? "Active" : "Inactive",
            ManufacturingProcesses = manufacturingProcesses,
            Color = string.Join(", ", colors.Select(color => color.Name)),
            Colors = colors,
            Properties = material.MechanicalProperties
                .Select(property => new MaterialPropertyDto
                {
                    Key = property.MechanicalPropertyName,
                    Value = property.Value.ToString("N2"),
                    Unit = property.Unit
                })
                .ToList(),
            Suppliers = material.SupplierId.HasValue
                ? [new SupplierSummaryDto { Id = material.SupplierId.Value, Name = material.SupplierName ?? "Supplier", Status = "Active" }]
                : [],
            CreatedAt = material.CreatedAt.UtcDateTime,
            UpdatedAt = material.UpdatedAt?.UtcDateTime ?? material.CreatedAt.UtcDateTime
        };
    }
}

/// <summary>
/// Paged result wrapper matching MaterialService's response shape.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class MaterialServicePagedResult<T>
{
    /// <summary>Items in the current page.</summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>Current page number.</summary>
    public int Page { get; set; }

    /// <summary>Page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Total result count.</summary>
    public int TotalCount { get; set; }

    /// <summary>Total page count.</summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// MaterialService wire DTO.
/// </summary>
public class MaterialServiceMaterialDto
{
    /// <summary>Material ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Material name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Material code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Material description.</summary>
    public string? Description { get; set; }

    /// <summary>Material price per unit.</summary>
    public decimal PricePerUnit { get; set; }

    /// <summary>Current stock level.</summary>
    public int StockLevel { get; set; }

    /// <summary>Supplier ID.</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>Supplier name.</summary>
    public string? SupplierName { get; set; }

    /// <summary>Manufacturing processes.</summary>
    public List<MaterialServiceNamedDto> ManufacturingProcesses { get; set; } = [];

    /// <summary>Available colors.</summary>
    public List<MaterialServiceNamedDto> AvailableColors { get; set; } = [];

    /// <summary>Post-processing methods.</summary>
    public List<MaterialServiceNamedDto> PostProcessingMethods { get; set; } = [];

    /// <summary>Mechanical properties.</summary>
    public List<MaterialServiceMechanicalPropertyDto> MechanicalProperties { get; set; } = [];

    /// <summary>Created timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Updated timestamp.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Whether the material is active.</summary>
    public bool Active { get; set; }
}

/// <summary>
/// MaterialService named child DTO.
/// </summary>
public class MaterialServiceNamedDto
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional hex color code for color DTOs.</summary>
    public string? HexCode { get; set; }
}

/// <summary>
/// MaterialService mechanical property DTO.
/// </summary>
public class MaterialServiceMechanicalPropertyDto
{
    /// <summary>Property identifier.</summary>
    public Guid MechanicalPropertyId { get; set; }

    /// <summary>Property name.</summary>
    public string MechanicalPropertyName { get; set; } = string.Empty;

    /// <summary>Unit.</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Value.</summary>
    public decimal Value { get; set; }
}

/// <summary>
/// MaterialService update request shape.
/// </summary>
public sealed class MaterialServiceUpdateMaterialRequest
{
    /// <summary>Name of the material.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Unique material code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Optional material description.</summary>
    public string? Description { get; set; }
    /// <summary>Price per unit.</summary>
    public decimal PricePerUnit { get; set; }
    /// <summary>Current stock level.</summary>
    public int StockLevel { get; set; }
    /// <summary>Optional supplier identifier.</summary>
    public Guid? SupplierId { get; set; }
    /// <summary>Manufacturing process identifiers to preserve.</summary>
    public List<Guid> ManufacturingProcessIds { get; set; } = [];
    /// <summary>Color identifiers to preserve.</summary>
    public List<Guid> ColorIds { get; set; } = [];
    /// <summary>Post-processing method identifiers to preserve.</summary>
    public List<Guid> PostProcessingMethodIds { get; set; } = [];
    /// <summary>Mechanical property values to preserve.</summary>
    public List<MaterialServiceMechanicalPropertyRequest> MechanicalProperties { get; set; } = [];
}

/// <summary>
/// MaterialService mechanical property update request shape.
/// </summary>
public sealed class MaterialServiceMechanicalPropertyRequest
{
    /// <summary>Mechanical property identifier.</summary>
    public Guid MechanicalPropertyId { get; set; }
    /// <summary>Material-specific property value.</summary>
    public decimal Value { get; set; }
}

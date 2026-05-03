using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Bff.Services;

/// <summary>
/// Maps SearchService wire DTOs to Intranet global search DTOs.
/// </summary>
public static class SearchResultMapper
{
    /// <summary>
    /// Converts a SearchService response to the Intranet BFF response shape.
    /// </summary>
    /// <param name="response">Downstream SearchService response.</param>
    /// <param name="query">Normalized query used by the BFF.</param>
    /// <returns>BFF search response.</returns>
    public static GlobalSearchResponseDto ToGlobalSearchResponse(SearchServiceResponseDto? response, string query)
    {
        if (response is null)
        {
            return new GlobalSearchResponseDto(query, 0, []);
        }

        var results = response.Results
            .Select(ToGlobalSearchResult)
            .ToList();

        return new GlobalSearchResponseDto(response.Query, results.Count, results);
    }

    private static GlobalSearchResultDto ToGlobalSearchResult(SearchServiceResultDto result)
    {
        return new GlobalSearchResultDto(
            result.Title,
            result.Subtitle,
            ResolveArea(result.SourceService, result.ResourceType),
            result.ResourceType,
            result.Status,
            ResolveHref(result),
            result.Score);
    }

    /// <summary>
    /// Resolves an Intranet route for a search result.
    /// </summary>
    /// <param name="result">SearchService result row.</param>
    /// <returns>An Intranet href that resolves to a page.</returns>
    public static string ResolveHref(SearchServiceResultDto result)
    {
        var type = Normalize(result.ResourceType);
        var id = result.ResourceId.Trim();
        var titleQuery = Uri.EscapeDataString(string.IsNullOrWhiteSpace(result.Title) ? id : result.Title);

        return type switch
        {
            "customer" or "customers" when IsGuid(id) => $"/customers/{id}",
            "customer" or "customers" => $"/customers?search={titleQuery}",
            "company" or "companies" => $"/customers?search={titleQuery}",

            "project" or "projects" when IsGuid(id) => $"/sales/projects/{id}",
            "project" or "projects" => $"/sales/projects?search={titleQuery}",
            "quotation" or "quotations" => $"/sales/projects?search={titleQuery}",
            "order" or "orders" => $"/sales/projects?search={titleQuery}",

            "invoice" or "invoices" when IsGuid(id) => $"/accounting/{id}",
            "invoice" or "invoices" => $"/accounting?search={titleQuery}",
            "account" or "accounts" => $"/accounting?search={titleQuery}",
            "journal" or "journals" => $"/accounting?search={titleQuery}",

            "purchaseorder" or "purchase-order" or "purchase-orders" when IsInteger(id) => $"/purchasing/{id}",
            "purchaseorder" or "purchase-order" or "purchase-orders" => $"/purchasing?search={titleQuery}",
            "supplier" or "suppliers" => $"/purchasing?search={titleQuery}",
            "material" or "materials" when IsGuid(id) => $"/mfg/materials/{id}",
            "material" or "materials" => $"/mfg/materials?search={titleQuery}",
            "equipment" or "equipments" when IsGuid(id) => $"/mfg/equipment/{id}",
            "equipment" or "equipments" => $"/mfg/equipment?search={titleQuery}",

            "employee" or "employees" or "team" or "teams" => "/hr/profile",
            "user" or "users" when IsGuid(id) => $"/iam/users/{id}",
            "user" or "users" => "/iam",
            "role" or "roles" => "/iam",

            _ => $"/search?query={titleQuery}"
        };
    }

    private static string ResolveArea(string sourceService, string resourceType)
    {
        var service = Normalize(sourceService);
        var type = Normalize(resourceType);

        if (service.Contains("customer", StringComparison.Ordinal) ||
            service.Contains("project", StringComparison.Ordinal) ||
            service.Contains("quotation", StringComparison.Ordinal) ||
            service.Contains("order", StringComparison.Ordinal) ||
            type is "customer" or "customers" or "company" or "companies" or "project" or "projects" or "quotation" or "quotations" or "order" or "orders")
        {
            return "Sales & CRM";
        }

        if (service.Contains("invoice", StringComparison.Ordinal) ||
            service.Contains("payment", StringComparison.Ordinal) ||
            service.Contains("receipt", StringComparison.Ordinal) ||
            service.Contains("accounting", StringComparison.Ordinal) ||
            type is "invoice" or "invoices" or "payment" or "payments" or "receipt" or "receipts" or "account" or "accounts" or "journal" or "journals")
        {
            return "Finance";
        }

        if (service.Contains("employee", StringComparison.Ordinal) ||
            service.Contains("career", StringComparison.Ordinal) ||
            service.Contains("leave", StringComparison.Ordinal) ||
            service.Contains("performance", StringComparison.Ordinal) ||
            service.Contains("compensation", StringComparison.Ordinal))
        {
            return "HR & People";
        }

        if (service.Contains("material", StringComparison.Ordinal) ||
            service.Contains("inventory", StringComparison.Ordinal) ||
            service.Contains("facility", StringComparison.Ordinal) ||
            service.Contains("job", StringComparison.Ordinal) ||
            service.Contains("purchase", StringComparison.Ordinal) ||
            service.Contains("supplier", StringComparison.Ordinal))
        {
            return "Manufacturing";
        }

        return "System";
    }

    private static bool IsGuid(string value) => Guid.TryParse(value, out _);

    private static bool IsInteger(string value) => int.TryParse(value, out var parsed) && parsed > 0;

    private static string Normalize(string value)
    {
        return value.Trim()
            .Replace("service", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("_", "-", StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}

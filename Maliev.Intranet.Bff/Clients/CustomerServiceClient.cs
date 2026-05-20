using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Customer microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
/// <param name="logger">The logger instance.</param>
public class CustomerServiceClient(HttpClient httpClient, ILogger<CustomerServiceClient> logger)
{
    private static readonly TimeSpan NonCriticalOnboardingStepTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Creates a customer with basic details (Company + Customer + Note).
    /// </summary>
    public virtual async Task<CustomerResponse?> CreateCustomerBasicAsync(CustomerOnboardingRequest request, CancellationToken ct = default)
    {
        // 1. Create Company if needed
        Guid? companyId = request.Customer.CompanyId;
        if (request.NewCompany != null && !companyId.HasValue)
        {
            var companyResponse = await httpClient.PostAsJsonAsync("/customer/v1/companies", request.NewCompany, ct);
            if (!companyResponse.IsSuccessStatusCode)
            {
                throw await CreateUpstreamExceptionAsync(
                    companyResponse,
                    "Company profile could not be created.",
                    ct);
            }

            var company = await companyResponse.Content.ReadFromJsonAsync<CompanyResponse>(ct);
            companyId = company?.Id
                ?? throw new HttpRequestException(
                    "Company profile could not be created because CustomerService returned an empty response.",
                    null,
                    companyResponse.StatusCode);
        }

        if (companyId.HasValue
            && request.CompanyBillingAddress != null
            && HasRequiredAddressFields(request.CompanyBillingAddress))
        {
            await CreateAddressesAsync(companyId.Value, [request.CompanyBillingAddress], "Company", ensureDefaultShipping: false, ct: ct);
        }

        // 2. Create Customer
        request.Customer.CompanyId = companyId;
        var customerResponse = await httpClient.PostAsJsonAsync("/customer/v1/customers", request.Customer, ct);
        if (!customerResponse.IsSuccessStatusCode)
        {
            throw await CreateUpstreamExceptionAsync(
                customerResponse,
                "Customer profile could not be created.",
                ct);
        }

        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>(ct);
        if (customer == null)
        {
            throw new HttpRequestException(
                "Customer profile was created but CustomerService returned an empty response.",
                null,
                customerResponse.StatusCode);
        }

        if (request.Addresses.Count > 0)
        {
            await CreateAddressesAsync(customer.Id, request.Addresses, ct: ct);
        }

        if (request.Documents.Count > 0)
        {
            var documents = await CreateDocumentsAsync("Customer", customer.Id, request.Documents, ct);
            if (request.Nda != null)
            {
                await CreateNdaWithDocumentsAsync(customer.Id, request.Nda, documents, ct);
            }
        }
        else if (request.Nda != null)
        {
            await CreateNdaWithDocumentsAsync(customer.Id, request.Nda, [], ct);
        }

        if (!string.IsNullOrEmpty(request.InternalNote))
        {
            await TryCreateInitialInternalNoteAsync(customer.Id, request.InternalNote, ct);
        }

        return customer;
    }

    private async Task TryCreateInitialInternalNoteAsync(Guid customerId, string noteText, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(NonCriticalOnboardingStepTimeout);

        try
        {
            var response = await httpClient.PostAsJsonAsync("/customer/v1/internal-notes", new CreateInternalNoteRequest
            {
                OwnerType = "Customer",
                OwnerId = customerId,
                NoteText = noteText
            }, timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Initial internal note creation failed for customer {CustomerId} with status {StatusCode}; customer creation will continue.",
                    customerId,
                    response.StatusCode);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Initial internal note creation timed out for customer {CustomerId}; customer creation will continue.", customerId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Initial internal note creation failed for customer {CustomerId}; customer creation will continue.", customerId);
        }
    }

    /// <summary>
    /// Creates multiple addresses for a customer.
    /// </summary>
    public virtual async Task<List<AddressResponse>> CreateAddressesAsync(
        Guid ownerId,
        List<CreateAddressRequest> addresses,
        string ownerType = "Customer",
        bool ensureDefaultShipping = true,
        CancellationToken ct = default)
    {
        var addressesToCreate = ensureDefaultShipping && string.Equals(ownerType, "Customer", StringComparison.OrdinalIgnoreCase)
            ? await EnsureDefaultShippingAddressAsync(ownerId, addresses, ct)
            : addresses;
        var results = new List<AddressResponse>();
        foreach (var address in addressesToCreate)
        {
            var response = await httpClient.PostAsJsonAsync("/customer/v1/addresses", new
            {
                ownerType,
                ownerId,
                type = address.Type,
                isDefault = address.IsDefault,
                addressLine1 = address.AddressLine1,
                addressLine2 = address.AddressLine2,
                addressLine3 = address.AddressLine3,
                district = address.District,
                city = address.City,
                stateProvince = address.StateProvince,
                postalCode = address.PostalCode,
                countryId = address.CountryId,
                recipientName = address.RecipientName,
                recipientPhone = address.RecipientPhone
            }, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AddressResponse>(ct);
                if (result != null) results.Add(result);
                continue;
            }

            throw await CreateUpstreamExceptionAsync(
                response,
                $"{address.Type} address could not be created.",
                ct);
        }
        return results;
    }

    private async Task<IReadOnlyList<CreateAddressRequest>> EnsureDefaultShippingAddressAsync(
        Guid customerId,
        IReadOnlyList<CreateAddressRequest> addresses,
        CancellationToken ct)
    {
        if (addresses.Count == 0 || addresses.Any(IsShippingAddress))
            return addresses;

        var billingAddress = addresses.FirstOrDefault(IsBillingAddress);
        if (billingAddress is null || !HasRequiredAddressFields(billingAddress))
            return addresses;

        try
        {
            var existingAddresses = await httpClient.GetFromJsonAsync<List<AddressResponse>>($"/customer/v1/addresses?ownerType=Customer&ownerId={customerId}", ct) ?? [];
            if (existingAddresses.Any(IsShippingAddress))
                return addresses;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not check existing shipping address for customer {CustomerId}; applying billing address fallback.", customerId);
        }

        var normalizedAddresses = addresses.ToList();
        normalizedAddresses.Add(CloneAsShippingAddress(billingAddress));
        return normalizedAddresses;
    }

    private static CreateAddressRequest CloneAsShippingAddress(CreateAddressRequest billingAddress)
    {
        return new CreateAddressRequest
        {
            Type = "Shipping",
            IsDefault = true,
            AddressLine1 = billingAddress.AddressLine1,
            AddressLine2 = billingAddress.AddressLine2,
            AddressLine3 = billingAddress.AddressLine3,
            District = billingAddress.District,
            City = billingAddress.City,
            StateProvince = billingAddress.StateProvince,
            PostalCode = billingAddress.PostalCode,
            CountryId = billingAddress.CountryId,
            RecipientName = billingAddress.RecipientName,
            RecipientPhone = billingAddress.RecipientPhone
        };
    }

    private static bool HasRequiredAddressFields(CreateAddressRequest address)
    {
        return !string.IsNullOrWhiteSpace(address.AddressLine1)
            && !string.IsNullOrWhiteSpace(address.City)
            && !string.IsNullOrWhiteSpace(address.StateProvince)
            && !string.IsNullOrWhiteSpace(address.PostalCode)
            && address.CountryId != Guid.Empty;
    }

    private static bool IsBillingAddress(CreateAddressRequest address) =>
        string.Equals(address.Type, "Billing", StringComparison.OrdinalIgnoreCase);

    private static bool IsShippingAddress(CreateAddressRequest address) =>
        string.Equals(address.Type, "Shipping", StringComparison.OrdinalIgnoreCase);

    private static bool IsShippingAddress(AddressResponse address) =>
        string.Equals(address.Type, "Shipping", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Retrieves a paged list of customers, optionally filtered by a search query.
    /// Sorted by creation date descending (newest first).
    /// </summary>
    public virtual async Task<PagedResponse<CustomerSummaryDto>?> GetCustomersAsync(
        string? query = null,
        string? segment = null,
        string? tier = null,
        bool includeDeleted = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var url = $"/customer/v1/customers?page={page}&pageSize={pageSize}&sortBy=createdAt&sortDirection=desc";
        if (!string.IsNullOrEmpty(query)) url += $"&query={Uri.EscapeDataString(query)}";
        if (!string.IsNullOrEmpty(segment)) url += $"&segment={Uri.EscapeDataString(segment)}";
        if (!string.IsNullOrEmpty(tier)) url += $"&tier={Uri.EscapeDataString(tier)}";
        if (includeDeleted) url += "&includeDeleted=true";

        var response = await httpClient.GetFromJsonAsync<CustomerServicePaginatedResponse<CustomerSummaryDto>>(url, ct);
        if (response == null) return new PagedResponse<CustomerSummaryDto>();

        return new PagedResponse<CustomerSummaryDto>
        {
            Data = response.Items,
            Meta = new PaginationMeta
            {
                CurrentPage = response.Page,
                PageSize = response.PageSize,
                TotalCount = response.TotalCount,
                TotalItems = response.TotalCount,
                TotalPages = response.TotalPages
            }
        };
    }

    private class CustomerServicePaginatedResponse<T>
    {
        [System.Text.Json.Serialization.JsonPropertyName("items")]
        public List<T> Items { get; set; } = new();
        [System.Text.Json.Serialization.JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("page")]
        public int Page { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("pageSize")]
        public int PageSize { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Retrieves detailed information for a single customer by ID, aggregating related data.
    /// </summary>
    public virtual async Task<CustomerDetailDto?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await httpClient.GetFromJsonAsync<CustomerDetailDto>($"/customer/v1/customers/{id}", ct);
        if (customer == null) return null;

        var addressesTask = httpClient.GetFromJsonAsync<List<AddressResponse>>($"/customer/v1/addresses?ownerType=Customer&ownerId={id}", ct);
        var notesTask = httpClient.GetFromJsonAsync<List<InternalNoteResponse>>($"/customer/v1/internal-notes?ownerType=Customer&ownerId={id}", ct);
        var ndasTask = httpClient.GetFromJsonAsync<List<NDAResponse>>($"/customer/v1/ndas/customer/{id}", ct);
        var docsTask = httpClient.GetFromJsonAsync<List<DocumentResponse>>($"/customer/v1/documents?ownerType=Customer&ownerId={id}", ct);

        Task<CompanySummaryDto?>? companyTask = null;
        Task<List<AddressResponse>?>? companyAddressesTask = null;
        if (customer.CompanyId.HasValue)
        {
            companyTask = httpClient.GetFromJsonAsync<CompanySummaryDto>($"/customer/v1/companies/{customer.CompanyId.Value}", ct);
            companyAddressesTask = httpClient.GetFromJsonAsync<List<AddressResponse>>($"/customer/v1/addresses?ownerType=Company&ownerId={customer.CompanyId.Value}", ct);
        }

        try { await Task.WhenAll(addressesTask, notesTask, ndasTask, docsTask, companyTask ?? Task.FromResult<CompanySummaryDto?>(null), companyAddressesTask ?? Task.FromResult<List<AddressResponse>?>(null)); }
        catch (Exception ex) { logger.LogError(ex, "Failed to fetch some related data for customer {CustomerId}", id); }

        if (addressesTask.IsCompletedSuccessfully) customer.Addresses = await addressesTask ?? [];
        if (notesTask.IsCompletedSuccessfully) customer.Notes = await notesTask ?? [];
        if (docsTask.IsCompletedSuccessfully) customer.Documents = await docsTask ?? [];
        if (ndasTask.IsCompletedSuccessfully) customer.Ndas = await ndasTask ?? [];

        if (companyTask != null && companyTask.IsCompletedSuccessfully)
        {
            var company = await companyTask;
            if (company != null)
            {
                customer.CompanyName = company.Name;
                customer.CompanyVatNumber = company.VatNumber;
                customer.CompanyRegistrationNumber = company.RegistrationNumber;
                customer.CompanyContactEmail = company.ContactEmail;
                customer.CompanyPhone = company.ContactPhone;
                customer.CompanySegment = company.Segment;
                customer.CompanyTier = company.Tier;

                if (companyAddressesTask != null && companyAddressesTask.IsCompletedSuccessfully)
                {
                    var companyAddresses = await companyAddressesTask;
                    customer.CompanyBillingAddress = companyAddresses?.FirstOrDefault(a => a.Type == "Billing" && a.IsDefault) ?? companyAddresses?.FirstOrDefault(a => a.Type == "Billing");
                }
            }
        }

        return customer;
    }

    /// <summary>
    /// Retrieves payment terms available for customer profiles.
    /// </summary>
    public virtual async Task<IReadOnlyList<PaymentTermDto>> GetPaymentTermsAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<PaymentTermDto>>("/customer/v1/customers/payment-terms", ct)
            ?? [];
    }

    /// <summary>
    /// Gets activity history for a customer with pagination or skip/take.
    /// </summary>
    public virtual async Task<PagedResponse<CustomerActivityResponse>> GetCustomerActivityAsync(Guid id, int? skip = null, int? take = null, int page = 1, int pageSize = 50, string? search = null, CancellationToken ct = default)
    {
        var url = $"/customer/v1/customers/{id}/history?page={page}&pageSize={pageSize}";
        if (skip.HasValue) url += $"&skip={skip.Value}";
        if (take.HasValue) url += $"&take={take.Value}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search.Trim())}";

        var response = await httpClient.GetFromJsonAsync<CustomerServicePaginatedResponse<CustomerActivityResponse>>(url, ct);
        if (response == null) return new PagedResponse<CustomerActivityResponse>();

        return new PagedResponse<CustomerActivityResponse>
        {
            Data = response.Items,
            Meta = new PaginationMeta
            {
                CurrentPage = response.Page,
                PageSize = response.PageSize,
                TotalCount = response.TotalCount,
                TotalItems = response.TotalCount,
                TotalPages = response.TotalPages
            }
        };
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    public virtual async Task<CustomerResponse?> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/customer/v1/customers", request, ct);
        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<CustomerResponse>(ct);
        return null;
    }

    /// <summary>
    /// Retrieves a paged list of companies.
    /// </summary>
    public virtual async Task<PagedResponse<CompanySummaryDto>?> GetCompaniesAsync(string? query = null, int page = 1, CancellationToken ct = default)
    {
        var url = $"/customer/v1/companies?page={page}&sortBy=name&sortDirection=asc";
        if (!string.IsNullOrEmpty(query)) url += $"&name={Uri.EscapeDataString(query)}";

        var response = await httpClient.GetFromJsonAsync<CustomerServicePaginatedResponse<CompanySummaryDto>>(url, ct);
        if (response == null) return new PagedResponse<CompanySummaryDto>();

        return new PagedResponse<CompanySummaryDto>
        {
            Data = response.Items,
            Meta = new PaginationMeta
            {
                CurrentPage = response.Page,
                PageSize = response.PageSize,
                TotalCount = response.TotalCount,
                TotalItems = response.TotalCount,
                TotalPages = response.TotalPages
            }
        };
    }

    /// <summary>
    /// Searches companies using the CustomerService unified internal/registry search endpoint.
    /// </summary>
    public virtual async Task<List<CompanySearchResultDto>> SearchCompanyResultsAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var response = await httpClient.GetAsync(
            $"/customer/v1/companies/search?query={Uri.EscapeDataString(query)}&limit={limit}",
            ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return document.RootElement
            .EnumerateArray()
            .Select(MapCompanySearchResult)
            .Where(company => !string.IsNullOrWhiteSpace(company.Name))
            .ToList();
    }

    private static CompanySearchResultDto MapCompanySearchResult(JsonElement element)
    {
        return new CompanySearchResultDto
        {
            Id = TryGetGuid(element, "id"),
            Name = GetString(element, "name") ?? string.Empty,
            VatNumber = GetString(element, "vatNumber"),
            RegistrationNumber = GetString(element, "registrationNumber"),
            ContactEmail = GetString(element, "contactEmail"),
            ContactPhone = GetString(element, "contactPhone"),
            Segment = GetString(element, "segment") ?? string.Empty,
            Tier = GetString(element, "tier") ?? string.Empty,
            Source = GetCompanySource(element),
            BusinessType = GetString(element, "businessType"),
            DefaultBillingAddress = TryGetProperty(element, "billingAddress", out var billingAddress)
                ? MapAddressResponse(billingAddress)
                : null
        };
    }

    private static AddressResponse? MapAddressResponse(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return new AddressResponse
        {
            Id = TryGetGuid(element, "id") ?? Guid.Empty,
            OwnerType = GetString(element, "ownerType") ?? string.Empty,
            OwnerId = TryGetGuid(element, "ownerId") ?? Guid.Empty,
            Type = GetString(element, "type") ?? "Billing",
            IsDefault = TryGetBool(element, "isDefault"),
            AddressLine1 = GetString(element, "addressLine1") ?? string.Empty,
            AddressLine2 = GetString(element, "addressLine2"),
            AddressLine3 = GetString(element, "addressLine3"),
            District = GetString(element, "district"),
            City = GetString(element, "city") ?? string.Empty,
            StateProvince = GetString(element, "stateProvince") ?? string.Empty,
            PostalCode = GetString(element, "postalCode") ?? string.Empty,
            CountryId = TryGetGuid(element, "countryId") ?? Guid.Empty,
            RecipientName = GetString(element, "recipientName"),
            RecipientPhone = GetString(element, "recipientPhone"),
            CreatedAt = TryGetDateTime(element, "createdAt"),
            UpdatedAt = TryGetDateTime(element, "updatedAt"),
            Xmin = TryGetUInt32(element, "xmin")
        };
    }

    private static string? GetCompanySource(JsonElement element)
    {
        if (!TryGetProperty(element, "source", out var source))
        {
            return null;
        }

        return source.ValueKind switch
        {
            JsonValueKind.String => source.GetString(),
            JsonValueKind.Number when source.TryGetInt32(out var value) => value switch
            {
                0 => "Internal",
                1 => "Registry",
                _ => value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            _ => null
        };
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.TryGetProperty(propertyName, out property))
        {
            return true;
        }

        foreach (var current in element.EnumerateObject())
        {
            if (string.Equals(current.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = current.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var property)
            || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ToString();
    }

    private static Guid? TryGetGuid(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var property)
            || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String && property.TryGetGuid(out var value))
        {
            return value;
        }

        return null;
    }

    private static bool TryGetBool(JsonElement element, string propertyName) =>
        TryGetProperty(element, propertyName, out var property)
        && property.ValueKind == JsonValueKind.True;

    private static DateTime TryGetDateTime(JsonElement element, string propertyName) =>
        TryGetProperty(element, propertyName, out var property)
        && property.ValueKind == JsonValueKind.String
        && property.TryGetDateTime(out var value)
            ? value
            : default;

    private static uint TryGetUInt32(JsonElement element, string propertyName) =>
        TryGetProperty(element, propertyName, out var property)
        && property.ValueKind == JsonValueKind.Number
        && property.TryGetUInt32(out var value)
            ? value
            : 0;

    /// <summary>
    /// Creates a company in CustomerService.
    /// </summary>
    public virtual async Task<CompanyResponse?> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/customer/v1/companies", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CompanyResponse>(ct);
    }

    /// <summary>
    /// Updates a customer profile.
    /// </summary>
    public virtual async Task<CustomerResponse?> UpdateCustomerAsync(Guid id, object request, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/customers/{id}", request, ct);
        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<CustomerResponse>(ct);
        return null;
    }

    /// <summary>
    /// Updates a customer with full details.
    /// </summary>
    public virtual async Task<CustomerResponse?> UpdateCustomerFullAsync(Guid id, CustomerOnboardingRequest request, CancellationToken ct = default)
    {
        var current = await httpClient.GetFromJsonAsync<CustomerDetailDto>($"/customer/v1/customers/{id}", ct);
        if (current == null) return null;

        var patchRequest = new
        {
            firstName = request.Customer.FirstName,
            lastName = request.Customer.LastName,
            email = request.Customer.Email,
            mobile = request.Customer.Mobile,
            extension = request.Customer.Extension,
            landline = request.Customer.Landline,
            segment = request.Customer.Segment,
            tier = request.Customer.Tier,
            preferredLanguage = request.Customer.PreferredLanguage,
            timezone = request.Customer.Timezone,
            communicationPreferences = request.Customer.CommunicationPreferences,
            paymentTerms = request.Customer.PaymentTerms,
            companyId = request.Customer.CompanyId,
            accountManagerEmployeeId = request.Customer.AccountManagerEmployeeId,
            clearAccountManager = request.Customer.AccountManagerEmployeeId is null,
            xmin = current.Xmin
        };

        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/customers/{id}", patchRequest, ct);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Customer update failed", null, response.StatusCode);

        var updatedCustomer = await response.Content.ReadFromJsonAsync<CustomerResponse>(ct);

        if (request.NewCompany != null)
        {
            if (request.Customer.CompanyId.HasValue)
                await httpClient.PatchAsJsonAsync($"/customer/v1/companies/{request.Customer.CompanyId.Value}", request.NewCompany, ct);
            else
            {
                var companyResponse = await httpClient.PostAsJsonAsync("/customer/v1/companies", request.NewCompany, ct);
                if (companyResponse.IsSuccessStatusCode)
                {
                    var company = await companyResponse.Content.ReadFromJsonAsync<CompanySummaryDto>(ct);
                    if (company != null)
                        await httpClient.PatchAsJsonAsync($"/customer/v1/customers/{id}", new { companyId = company.Id, xmin = updatedCustomer?.Xmin ?? current.Xmin }, ct);
                }
            }
        }

        var existingAddresses = await httpClient.GetFromJsonAsync<List<AddressResponse>>($"/customer/v1/addresses?ownerType=Customer&ownerId={id}", ct) ?? [];
        var requestAddressIds = request.Addresses.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToList();
        var addressesToDelete = existingAddresses.Where(a => !requestAddressIds.Contains(a.Id)).ToList();

        foreach (var addr in addressesToDelete)
        {
            var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/addresses/{addr.Id}") { Content = JsonContent.Create(new { version = addr.Version }) };
            await httpClient.SendAsync(delReq, ct);
        }

        foreach (var address in request.Addresses)
        {
            var addrReq = new { ownerType = "Customer", ownerId = id, type = address.Type, isDefault = address.IsDefault, addressLine1 = address.AddressLine1, addressLine2 = address.AddressLine2, addressLine3 = address.AddressLine3, district = address.District, city = address.City, stateProvince = address.StateProvince, postalCode = address.PostalCode, countryId = address.CountryId, recipientName = address.RecipientName, recipientPhone = address.RecipientPhone, version = address.Version };
            if (address.Id.HasValue) await httpClient.PatchAsJsonAsync($"/customer/v1/addresses/{address.Id.Value}", addrReq, ct);
            else await httpClient.PostAsJsonAsync("/customer/v1/addresses", addrReq, ct);
        }

        return updatedCustomer;
    }

    /// <summary>
    /// Adds an internal note.
    /// </summary>
    public virtual async Task<InternalNoteResponse?> AddInternalNoteAsync(Guid ownerId, CreateInternalNoteRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/customer/v1/internal-notes", request, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<InternalNoteResponse>(ct) : null;
    }

    /// <summary>
    /// Updates an internal note.
    /// </summary>
    public virtual async Task<InternalNoteResponse?> UpdateInternalNoteAsync(Guid id, UpdateInternalNoteRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/internal-notes/{id}", request, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<InternalNoteResponse>(ct) : null;
    }

    /// <summary>
    /// Deletes an internal note.
    /// </summary>
    public virtual async Task<bool> DeleteInternalNoteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/customer/v1/internal-notes/{id}", ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Adds a comment to a note.
    /// </summary>
    public virtual async Task<InternalNoteCommentResponse?> AddInternalNoteCommentAsync(Guid noteId, CreateInternalNoteCommentRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/customer/v1/internal-notes/{noteId}/comments", request, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<InternalNoteCommentResponse>(ct) : null;
    }

    /// <summary>
    /// Gets activity for a note.
    /// </summary>
    public virtual async Task<List<object>> GetInternalNoteActivityAsync(Guid noteId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<object>>($"/customer/v1/internal-notes/{noteId}/activity", ct) ?? [];
    }

    /// <summary>
    /// Updates NDA status.
    /// </summary>
    public virtual async Task<bool> UpdateNdaStatusAsync(Guid ndaId, object updateRequest, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/ndas/{ndaId}/status", updateRequest, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Updates NDA details (expiration, etc.).
    /// </summary>
    public virtual async Task<bool> UpdateNdaAsync(Guid ndaId, object updateRequest, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/ndas/{ndaId}", updateRequest, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Creates a paged list of countries.
    /// </summary>
    public virtual async Task<List<CountryDto>> GetCountriesAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<PagedResponse<CountryDto>>("/customer/v1/countries", ct);
        return response?.Data.ToList() ?? [];
    }

    /// <summary>
    /// Creates a single company by ID.
    /// </summary>
    public virtual async Task<CompanyResponse?> GetCompanyByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<CompanyResponse>($"/customer/v1/companies/{id}", ct);
    }

    /// <summary>
    /// Creates an NDA record.
    /// </summary>
    public virtual async Task<bool> CreateNdaAsync(object ndaRequest, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/customer/v1/ndas", ndaRequest, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Deletes an NDA record.
    /// </summary>
    public virtual async Task<bool> DeleteNdaAsync(Guid ndaId, byte[] version, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/ndas/{ndaId}") { Content = JsonContent.Create(new { version }) };
        var response = await httpClient.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Gets audit history for an NDA.
    /// </summary>
    public virtual async Task<List<NDAAuditLogResponse>> GetNdaHistoryAsync(Guid ndaId, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<NDAAuditLogResponse>>($"/customer/v1/ndas/{ndaId}/history", ct) ?? [];
    }

    /// <summary>
    /// Searches for companies.
    /// </summary>
    public virtual async Task<List<CompanySummaryDto>> SearchCompaniesAsync(string? query = null, CancellationToken ct = default)
    {
        var url = "/customer/v1/companies";
        if (!string.IsNullOrEmpty(query)) url += $"?name={Uri.EscapeDataString(query)}";
        var response = await httpClient.GetFromJsonAsync<PagedResponse<CompanySummaryDto>>(url, ct);
        return response?.Data.ToList() ?? [];
    }

    /// <summary>
    /// Creates documents for an owner.
    /// </summary>
    public virtual async Task<List<DocumentResponse>> CreateDocumentsAsync(string ownerType, Guid ownerId, List<CreateDocumentRequest> documents, CancellationToken ct = default)
    {
        var results = new List<DocumentResponse>();
        foreach (var doc in documents)
        {
            var response = await httpClient.PostAsJsonAsync("/customer/v1/documents", new
            {
                ownerType,
                ownerId,
                documentType = doc.DocumentCategory,
                documentSubType = doc.DocumentSubType,
                fileReference = doc.FileReference,
                filename = doc.FileName,
                fileSize = doc.FileSize,
                mimeType = doc.MimeType,
                description = doc.Description,
                displayOrder = doc.DisplayOrder
            }, ct);
            if (response.IsSuccessStatusCode) { var result = await response.Content.ReadFromJsonAsync<DocumentResponse>(ct); if (result != null) results.Add(result); }
        }
        return results;
    }

    /// <summary>
    /// Deletes a document.
    /// </summary>
    public virtual async Task<bool> DeleteDocumentAsync(Guid documentId, byte[] rowVersion, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/documents/{documentId}") { Content = JsonContent.Create(new { version = rowVersion }) };
        var response = await httpClient.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Creates NDA for a customer, linking documents if available.
    /// </summary>
    public virtual async Task CreateNdaWithDocumentsAsync(Guid customerId, CreateNDARequest nda, List<DocumentResponse> documents, CancellationToken ct = default)
    {
        var signedNdaDoc = documents.FirstOrDefault(d => d.DocumentCategory == "NDA" && d.DocumentSubType == "Signed");
        var ndaResponse = await httpClient.PostAsJsonAsync("/customer/v1/ndas", new
        {
            customerId,
            documentReferenceId = signedNdaDoc?.Id,
            expiresAt = nda.ExpiresAt
        }, ct);

        if (!ndaResponse.IsSuccessStatusCode)
        {
            throw await CreateUpstreamExceptionAsync(ndaResponse, "NDA record could not be created.", ct);
        }

        var createdNda = await ndaResponse.Content.ReadFromJsonAsync<NDAResponse>(ct);
        if (createdNda != null && signedNdaDoc != null && nda.IsActive)
        {
            var statusResponse = await httpClient.PatchAsJsonAsync($"/customer/v1/ndas/{createdNda.Id}/status", new
            {
                status = "Signed",
                signedAt = DateTime.UtcNow,
                documentReferenceId = signedNdaDoc.Id,
                expiresAt = nda.ExpiresAt,
                xmin = createdNda.Xmin
            }, ct);

            if (!statusResponse.IsSuccessStatusCode)
            {
                throw await CreateUpstreamExceptionAsync(statusResponse, "NDA status could not be updated.", ct);
            }
        }
    }

    /// <summary>
    /// Updates a single address via PATCH.
    /// </summary>
    public virtual async Task<bool> UpdateAddressAsync(Guid addressId, UpdateAddressRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/addresses/{addressId}", new
        {
            type = request.Type,
            isDefault = request.IsDefault,
            addressLine1 = request.AddressLine1,
            addressLine2 = request.AddressLine2,
            addressLine3 = request.AddressLine3,
            district = request.District,
            city = request.City,
            stateProvince = request.StateProvince,
            postalCode = request.PostalCode,
            countryId = request.CountryId,
            recipientName = request.RecipientName,
            recipientPhone = request.RecipientPhone,
            xmin = request.Xmin
        }, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Deletes a customer address.
    /// </summary>
    public virtual async Task<bool> DeleteAddressAsync(Guid addressId, uint xmin, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/addresses/{addressId}")
        {
            Content = JsonContent.Create(new { xmin })
        };
        var response = await httpClient.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    private static async Task<HttpRequestException> CreateUpstreamExceptionAsync(
        HttpResponseMessage response,
        string fallback,
        CancellationToken ct)
    {
        var message = await ReadUpstreamErrorMessageAsync(response, fallback, ct);
        return new HttpRequestException(message, null, response.StatusCode);
    }

    private static async Task<string> ReadUpstreamErrorMessageAsync(
        HttpResponseMessage response,
        string fallback,
        CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"{fallback} HTTP {(int)response.StatusCode} {response.ReasonPhrase}".Trim();
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var parts = new List<string>();

            AddJsonString(parts, root, "message");
            AddJsonString(parts, root, "title");
            AddJsonDetails(parts, root, "details");
            AddJsonDetails(parts, root, "errors");

            if (parts.Count > 0)
            {
                return string.Join(" ", parts.Distinct(StringComparer.OrdinalIgnoreCase));
            }
        }
        catch (JsonException)
        {
            // Fall back to the raw upstream body below.
        }

        return body.Length <= 1000 ? body : $"{body[..1000]}...";
    }

    private static void AddJsonString(List<string> parts, JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            var message = value.GetString();
            if (!string.IsNullOrWhiteSpace(message))
            {
                parts.Add(message.Trim());
            }
        }
    }

    private static void AddJsonDetails(List<string> parts, JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var details) || details.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in details.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                var messages = property.Value
                    .EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString())
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Select(message => message!.Trim());

                parts.AddRange(messages);
            }
            else if (property.Value.ValueKind == JsonValueKind.String)
            {
                var message = property.Value.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    parts.Add(message.Trim());
                }
            }
        }
    }

    /// <summary>
    /// Promotes a customer to be the primary contact for their company.
    /// </summary>
    public virtual async Task<bool> PromotePrimaryContactAsync(Guid companyId, Guid customerId, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/customer/v1/companies/{companyId}/primary-contact/{customerId}", null, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Updates a company by ID.
    /// </summary>
    public virtual async Task<CompanyResponse?> UpdateCompanyAsync(Guid id, UpdateCompanyRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/companies/{id}", request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CompanyResponse>(ct);
    }
}

using Maliev.Intranet.Shared;
using System.Net.Http.Json;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Customer microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
/// <param name="logger">The logger instance.</param>
public class CustomerServiceClient(HttpClient httpClient, ILogger<CustomerServiceClient> logger)
{
    /// <summary>
    /// Creates a customer with basic details (Company + Customer + Note).
    /// </summary>
    public virtual async Task<CustomerResponse?> CreateCustomerBasicAsync(CustomerOnboardingRequest request, CancellationToken ct = default)
    {
        // 1. Create Company if needed
        Guid? companyId = null;
        if (request.NewCompany != null)
        {
            var companyResponse = await httpClient.PostAsJsonAsync("/customer/v1/companies", request.NewCompany, ct);
            if (companyResponse.IsSuccessStatusCode)
            {
                var company = await companyResponse.Content.ReadFromJsonAsync<CompanyResponse>(ct);
                companyId = company?.Id;
            }
        }

        // 2. Create Customer
        request.Customer.CompanyId = companyId;
        var customerResponse = await httpClient.PostAsJsonAsync("/customer/v1/customers", request.Customer, ct);
        if (!customerResponse.IsSuccessStatusCode) return null;

        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>(ct);
        if (customer == null) return null;

        // 3. Create Note if needed
        if (!string.IsNullOrEmpty(request.InternalNote))
        {
            await httpClient.PostAsJsonAsync("/customer/v1/internal-notes", new CreateInternalNoteRequest
            {
                OwnerType = "Customer",
                OwnerId = customer.Id,
                NoteText = request.InternalNote
            }, ct);
        }

        return customer;
    }

    /// <summary>
    /// Creates multiple addresses for a customer.
    /// </summary>
    public virtual async Task<List<AddressResponse>> CreateAddressesAsync(Guid customerId, List<CreateAddressRequest> addresses, CancellationToken ct = default)
    {
        var addressesToCreate = await EnsureDefaultShippingAddressAsync(customerId, addresses, ct);
        var results = new List<AddressResponse>();
        foreach (var address in addressesToCreate)
        {
            var response = await httpClient.PostAsJsonAsync("/customer/v1/addresses", new
            {
                ownerType = "Customer",
                ownerId = customerId,
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
            }
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
    /// Gets activity history for a customer with pagination or skip/take.
    /// </summary>
    public virtual async Task<PagedResponse<CustomerActivityResponse>> GetCustomerActivityAsync(Guid id, int? skip = null, int? take = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var url = $"/customer/v1/customers/{id}/history?page={page}&pageSize={pageSize}";
        if (skip.HasValue) url += $"&skip={skip.Value}";
        if (take.HasValue) url += $"&take={take.Value}";

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

        var response = await httpClient.GetFromJsonAsync<List<CompanySearchResultDto>>(
            $"/customer/v1/companies/search?query={Uri.EscapeDataString(query)}&limit={limit}", ct);
        return response ?? [];
    }

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
            var response = await httpClient.PostAsJsonAsync("/customer/v1/documents", new { ownerType, ownerId, documentType = doc.DocumentCategory, fileReference = doc.FileReference, filename = doc.FileName, fileSize = doc.FileSize, mimeType = doc.MimeType }, ct);
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
        var ndaResponse = await httpClient.PostAsJsonAsync("/customer/v1/ndas", new { customerId, documentReferenceId = signedNdaDoc?.Id, expiresAt = nda.ExpiresAt }, ct);
        if (ndaResponse.IsSuccessStatusCode)
        {
            var ndaJson = await ndaResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
            if (ndaJson.ValueKind != System.Text.Json.JsonValueKind.Undefined && ndaJson.TryGetProperty("id", out var idProp) && ndaJson.TryGetProperty("version", out var vProp))
                await httpClient.PatchAsJsonAsync($"/customer/v1/ndas/{idProp.GetGuid()}/status", new { status = "Signed", version = vProp.GetString() }, ct);
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

    private static string ExtractErrorMessage(string errorBody, string fallback) => fallback;

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

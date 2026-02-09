using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Customer microservice.
/// </summary>
/// <param name="httpClient">The HTTP client instance.</param>
/// <param name="logger">The logger instance.</param>
public class CustomerServiceClient(HttpClient httpClient, ILogger<CustomerServiceClient> logger)
{
    /// <summary>
    /// Retrieves a paged list of customers, optionally filtered by a search query.
    /// Sorted by creation date descending (newest first).
    /// </summary>
    /// <param name="query">The search query to filter customers.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A paged response containing customer summaries.</returns>
    public async Task<PagedResponse<CustomerSummaryDto>?> GetCustomersAsync(string? query = null, int page = 1, CancellationToken ct = default)
    {
        var url = $"/customer/v1/customers?page={page}&sortBy=createdAt&sortDirection=desc";
        if (!string.IsNullOrEmpty(query)) url += $"&query={Uri.EscapeDataString(query)}";

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

    /// <summary>
    /// Helper DTO to match downstream Customer Service paginated response.
    /// </summary>
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
    /// <param name="id">The customer ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The customer detail DTO.</returns>
    public async Task<CustomerDetailDto?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await httpClient.GetFromJsonAsync<CustomerDetailDto>($"/customer/v1/customers/{id}", ct);
        if (customer == null) return null;

        // Fetch related data in parallel
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
        catch (Exception ex) { logger.LogWarning(ex, "Error fetching some related data for customer {CustomerId}", id); }

        customer.Addresses = addressesTask.IsCompletedSuccessfully ? (addressesTask.Result ?? []) : [];
        customer.Notes = notesTask.IsCompletedSuccessfully ? (notesTask.Result ?? []) : [];
        customer.Documents = docsTask.IsCompletedSuccessfully ? (docsTask.Result ?? []) : [];
        
        if (ndasTask.IsCompletedSuccessfully && ndasTask.Result != null)
        {
            customer.Nda = ndasTask.Result.OrderByDescending(n => n.CreatedAt).FirstOrDefault();
        }

        if (companyTask != null && companyTask.IsCompletedSuccessfully && companyTask.Result != null)
        {
            var company = companyTask.Result;
            customer.CompanyName = company.Name;
            customer.CompanyVatNumber = company.VatNumber;
            customer.CompanyRegistrationNumber = company.RegistrationNumber;
            customer.CompanyContactEmail = company.ContactEmail;
            customer.CompanyPhone = company.ContactPhone;
            customer.CompanySegment = company.Segment;
            customer.CompanyTier = company.Tier;

            if (companyAddressesTask != null && companyAddressesTask.IsCompletedSuccessfully)
            {
                customer.CompanyBillingAddress = companyAddressesTask.Result?.FirstOrDefault(a => a.Type == "Billing" && a.IsDefault) 
                                              ?? companyAddressesTask.Result?.FirstOrDefault(a => a.Type == "Billing");
            }
        }

        return customer;
    }

    /// <summary>
    /// Creates a new customer in the Customer microservice.
    /// </summary>
    /// <param name="request">The customer creation request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created customer details.</returns>
    public async Task<CustomerResponse?> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/customer/v1/customers", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CustomerResponse>(ct);
        }
        return null;
    }

    /// <summary>
    /// Updates an existing customer with optional company and addresses.
    /// </summary>
    public async Task<CustomerResponse?> UpdateCustomerFullAsync(Guid id, CustomerOnboardingRequest request, CancellationToken ct = default)
    {
        // 1. Update basic customer info
        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/customers/{id}", request.Customer, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Customer update failed ({StatusCode}): {ErrorBody}", response.StatusCode, errorBody);
            return null;
        }
        var updatedCustomer = await response.Content.ReadFromJsonAsync<CustomerResponse>(ct);

        // 2. Update/Create company if provided
        if (request.NewCompany != null)
        {
            if (request.Customer.CompanyId.HasValue)
            {
                await httpClient.PatchAsJsonAsync($"/customer/v1/companies/{request.Customer.CompanyId.Value}", request.NewCompany, ct);
            }
            else
            {
                var companyResponse = await httpClient.PostAsJsonAsync("/customer/v1/companies", request.NewCompany, ct);
                if (companyResponse.IsSuccessStatusCode)
                {
                    var company = await companyResponse.Content.ReadFromJsonAsync<CompanySummaryDto>(ct);
                    if (company != null)
                    {
                        await httpClient.PatchAsJsonAsync($"/customer/v1/customers/{id}", new { companyId = company.Id }, ct);
                    }
                }
            }
        }

        // 3. Update addresses (simplified: delete existing and create new for now, or just add new ones)
        // In a real scenario, we'd diff them. For this prototype, we'll just ensure they are created.
        foreach (var address in request.Addresses)
        {
            var addrReq = new 
            {
                ownerType = "Customer",
                ownerId = id,
                type = address.Type,
                isDefault = true,
                addressLine1 = address.AddressLine1,
                addressLine2 = address.AddressLine2,
                addressLine3 = address.AddressLine3,
                district = address.District,
                city = address.City,
                stateProvince = address.StateProvince,
                postalCode = address.PostalCode,
                countryId = address.CountryId
            };
            await httpClient.PostAsJsonAsync("/customer/v1/addresses", addrReq, ct);
        }

        return updatedCustomer;
    }

    /// <summary>
    /// Onboards a new customer with optional company and addresses.
    /// </summary>
    public async Task<CustomerResponse?> OnboardCustomerAsync(CustomerOnboardingRequest request, CancellationToken ct = default)
    {
        // This is a composite operation. Since the downstream service doesn't have a single "onboard" endpoint,
        // we'll orchestrate it here in the BFF.
        
        Guid? companyId = request.Customer.CompanyId;

        // 1. Create company if provided
        if (request.NewCompany != null)
        {
            var companyResponse = await httpClient.PostAsJsonAsync("/customer/v1/companies", request.NewCompany, ct);
            if (companyResponse.IsSuccessStatusCode)
            {
                var company = await companyResponse.Content.ReadFromJsonAsync<CompanySummaryDto>(ct);
                companyId = company?.Id;
            }
            else 
            {
                var errorBody = await companyResponse.Content.ReadAsStringAsync(ct);
                logger.LogError("Company creation failed ({StatusCode}): {ErrorBody}", companyResponse.StatusCode, errorBody);
                return null;
            }
        }

        // 2. Create customer
        request.Customer.CompanyId = companyId;
        var customerResponse = await httpClient.PostAsJsonAsync("/customer/v1/customers", request.Customer, ct);
        if (!customerResponse.IsSuccessStatusCode)
        {
            var errorBody = await customerResponse.Content.ReadAsStringAsync(ct);
            logger.LogError("Customer creation failed ({StatusCode}): {ErrorBody}", customerResponse.StatusCode, errorBody);
            return null;
        }
        
        var createdCustomer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>(ct);
        if (createdCustomer == null) return null;

        // 3. Create internal note if provided
        if (!string.IsNullOrWhiteSpace(request.InternalNote))
        {
            var noteReq = new
            {
                ownerType = "Customer",
                ownerId = createdCustomer.Id,
                noteText = request.InternalNote
            };
            await httpClient.PostAsJsonAsync("/customer/v1/internal-notes", noteReq, ct);
        }

        // 4. Create addresses
        foreach (var address in request.Addresses)
        {
            var addrReq = new 
            {
                ownerType = "Customer",
                ownerId = createdCustomer.Id,
                type = address.Type,
                isDefault = true,
                addressLine1 = address.AddressLine1,
                addressLine2 = address.AddressLine2,
                addressLine3 = address.AddressLine3,
                district = address.District,
                city = address.City,
                stateProvince = address.StateProvince,
                postalCode = address.PostalCode,
                countryId = address.CountryId
            };
            var addrResponse = await httpClient.PostAsJsonAsync("/customer/v1/addresses", addrReq, ct);
            if (!addrResponse.IsSuccessStatusCode)
            {
                var errorBody = await addrResponse.Content.ReadAsStringAsync(ct);
                logger.LogError("Address creation failed ({StatusCode}): {ErrorBody}", addrResponse.StatusCode, errorBody);
            }
        }

        // 5. Create all documents (NDA and customer documents)
        if (request.Documents.Any())
        {
            var createdDocuments = await CreateDocumentsAsync(
                "Customer", createdCustomer.Id, request.Documents, ct);

            // 5a. If NDA is active, link the signed document
            if (request.Nda != null && request.Nda.IsActive)
            {
                var signedNdaDoc = createdDocuments
                    .FirstOrDefault(d => d.DocumentCategory == "NDA" &&
                                        d.DocumentSubType == "Signed");

                var ndaReq = new
                {
                    customerId = createdCustomer.Id,
                    documentReferenceId = signedNdaDoc?.Id,
                    expiresAt = request.Nda.ExpiresAt
                };

                var ndaResponse = await httpClient.PostAsJsonAsync("/customer/v1/ndas", ndaReq, ct);
                if (ndaResponse.IsSuccessStatusCode)
                {
                    // Update status to Signed
                    var nda = await ndaResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
                    if (nda.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                        nda.TryGetProperty("id", out var ndaIdProp) &&
                        nda.TryGetProperty("version", out var versionProp))
                    {
                        Guid ndaId = ndaIdProp.GetGuid();
                        await httpClient.PatchAsJsonAsync(
                            $"/customer/v1/ndas/{ndaId}/status",
                            new { status = "Signed", version = versionProp.GetString() }, ct);
                    }
                }
            }
        }

        return createdCustomer;
    }

    /// <summary>
    /// Creates multiple documents for an owner.
    /// </summary>
    /// <param name="ownerType">Type of owner (e.g., "Customer", "Company").</param>
    /// <param name="ownerId">ID of the owner entity.</param>
    /// <param name="documents">List of documents to create.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>List of created document responses.</returns>
    public async Task<List<DocumentResponse>> CreateDocumentsAsync(
        string ownerType,
        Guid ownerId,
        List<CreateDocumentRequest> documents,
        CancellationToken ct = default)
    {
        var results = new List<DocumentResponse>();

        foreach (var doc in documents)
        {
            var docReq = new
            {
                ownerType,
                ownerId,
                documentCategory = doc.DocumentCategory,
                documentSubType = doc.DocumentSubType,
                fileReference = doc.FileReference,
                fileName = doc.FileName,
                fileSize = doc.FileSize,
                mimeType = doc.MimeType,
                description = doc.Description,
                displayOrder = doc.DisplayOrder
            };

            var response = await httpClient.PostAsJsonAsync("/customer/v1/documents", docReq, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DocumentResponse>(ct);
                if (result != null) results.Add(result);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Document creation failed ({StatusCode}): {ErrorBody}", response.StatusCode, errorBody);
            }
        }

        return results;
    }

    /// <summary>
    /// Gets all documents for a customer.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>List of document responses.</returns>
    public async Task<List<DocumentResponse>> GetCustomerDocumentsAsync(
        Guid customerId,
        string? category = null,
        CancellationToken ct = default)
    {
        var url = $"/customer/v1/documents?ownerType=Customer&ownerId={customerId}";
        if (!string.IsNullOrEmpty(category))
            url += $"&category={Uri.EscapeDataString(category)}";

        return await httpClient.GetFromJsonAsync<List<DocumentResponse>>(url, ct) ?? [];
    }

    /// <summary>
    /// Deletes a document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="version">The document version for optimistic concurrency.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> DeleteDocumentAsync(
        Guid documentId,
        byte[] version,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/documents/{documentId}")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { version })
        };

        var response = await httpClient.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Gets the list of available countries.
    /// </summary>
    public async Task<List<CountryDto>> GetCountriesAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetFromJsonAsync<PagedResponse<CountryDto>>("/customer/v1/countries", ct);
        return response?.Data.ToList() ?? new List<CountryDto>();
    }

    /// <summary>
    /// Searches for companies in the Customer microservice.
    /// </summary>
    /// <param name="query">The search query for company name or VAT.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of company summaries.</returns>
    public async Task<List<CompanySummaryDto>> SearchCompaniesAsync(string? query = null, CancellationToken ct = default)
    {
        var url = "/customer/v1/companies";
        if (!string.IsNullOrEmpty(query)) url += $"?name={Uri.EscapeDataString(query)}";

        var response = await httpClient.GetFromJsonAsync<PagedResponse<CompanySummaryDto>>(url, ct);
        return response?.Data.ToList() ?? new List<CompanySummaryDto>();
    }

    /// <summary>
    /// Searches for companies with their default billing addresses.
    /// </summary>
    public async Task<List<CompanySearchResultDto>> SearchCompaniesWithAddressAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        var url = $"/customer/v1/companies/search?query={Uri.EscapeDataString(query)}&limit={limit}";
        return await httpClient.GetFromJsonAsync<List<CompanySearchResultDto>>(url, ct) ?? [];
    }

    /// <summary>
    /// Checks if a customer with the specified email already exists.
    /// </summary>
    /// <param name="email">Email address to check.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if email exists, false otherwise.</returns>
    public async Task<bool> CheckEmailExistsAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        var url = $"/customer/v1/customers/check-email?email={Uri.EscapeDataString(email.Trim())}";
        var response = await httpClient.GetFromJsonAsync<EmailExistsResponse>(url, ct);
        return response?.Exists ?? false;
    }
}

/// <summary>
/// Response model for email existence check.
/// </summary>
public class EmailExistsResponse
{
    /// <summary>
    /// Indicates whether a customer with this email already exists.
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// The normalized email that was checked.
    /// </summary>
    public string? Email { get; set; }
}

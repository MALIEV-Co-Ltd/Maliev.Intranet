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

        try 
        { 
            await Task.WhenAll(addressesTask, notesTask, ndasTask, docsTask, 
                              companyTask ?? Task.FromResult<CompanySummaryDto?>(null), 
                              companyAddressesTask ?? Task.FromResult<List<AddressResponse>?>(null)); 
        }
        catch (Exception ex) 
        { 
            logger.LogError(ex, "Failed to fetch some related data for customer {CustomerId}", id); 
        }

        if (addressesTask.IsCompletedSuccessfully) customer.Addresses = await addressesTask ?? [];
        else logger.LogWarning("Addresses task failed for customer {CustomerId}", id);

        if (notesTask.IsCompletedSuccessfully) customer.Notes = await notesTask ?? [];
        else logger.LogWarning("Notes task failed for customer {CustomerId}", id);

        if (docsTask.IsCompletedSuccessfully) customer.Documents = await docsTask ?? [];
        else logger.LogWarning("Documents task failed for customer {CustomerId}", id);
        
        if (ndasTask.IsCompletedSuccessfully)
        {
            var ndas = await ndasTask;
            if (ndas != null && ndas.Any())
            {
                customer.Nda = ndas.OrderByDescending(n => n.CreatedAt).FirstOrDefault();
            }
        }
        else logger.LogWarning("NDAs task failed for customer {CustomerId}", id);

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
                    customer.CompanyBillingAddress = companyAddresses?.FirstOrDefault(a => a.Type == "Billing" && a.IsDefault) 
                                                  ?? companyAddresses?.FirstOrDefault(a => a.Type == "Billing");
                }
            }
        }

        return customer;
    }

    /// <summary>
    /// Gets activity history for a customer.
    /// </summary>
    public async Task<List<CustomerActivityResponse>> GetCustomerActivityAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<CustomerActivityResponse>>($"/customer/v1/customers/{id}/history", ct) ?? [];
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
    /// Updates a customer profile.
    /// </summary>
    public async Task<CustomerResponse?> UpdateCustomerAsync(Guid id, object request, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/customers/{id}", request, ct);
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
        // Fetch current version first or expect it in request. 
        // For simplicity and to match the CreateCustomerRequest pattern, we'll try to use the version from the CustomerOnboardingRequest if available.
        // But CustomerOnboardingRequest.Customer is CreateCustomerRequest which doesn't have version.
        // We'll need to fetch the customer first to get the version, OR change the DTO.
        
        var current = await httpClient.GetFromJsonAsync<CustomerDetailDto>($"/customer/v1/customers/{id}", ct);
        if (current == null) return null;

        // 1. Update basic customer info using Patch
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
            version = current.Version
        };

        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/customers/{id}", patchRequest, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Customer update failed ({StatusCode}): {ErrorBody}", response.StatusCode, errorBody);
            
            string message = "Customer update failed";
            try {
                var apiErr = System.Text.Json.JsonSerializer.Deserialize<ApiErrorResponse>(errorBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiErr != null && !string.IsNullOrEmpty(apiErr.Message)) message = apiErr.Message;
            } catch {}
            
            throw new HttpRequestException(message, null, response.StatusCode);
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
                        // Update customer with new companyId
                        await httpClient.PatchAsJsonAsync($"/customer/v1/customers/{id}", new { companyId = company.Id, version = updatedCustomer?.Version }, ct);
                    }
                }
            }
        }

        // 3. Update addresses
        var existingAddresses = await httpClient.GetFromJsonAsync<List<AddressResponse>>($"/customer/v1/addresses?ownerType=Customer&ownerId={id}", ct) ?? [];
        
        // Find addresses to delete (exist in DB but not in request)
        var requestAddressIds = request.Addresses.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToList();
        var addressesToDelete = existingAddresses.Where(a => !requestAddressIds.Contains(a.Id)).ToList();
        
        foreach (var addr in addressesToDelete)
        {
            var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/addresses/{addr.Id}")
            {
                Content = System.Net.Http.Json.JsonContent.Create(new { version = addr.Version })
            };
            await httpClient.SendAsync(delReq, ct);
        }

        foreach (var address in request.Addresses)
        {
            var addrReq = new 
            {
                ownerType = "Customer",
                ownerId = id,
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
                recipientPhone = address.RecipientPhone,
                version = address.Version
            };

            if (address.Id.HasValue)
            {
                // Update existing
                await httpClient.PatchAsJsonAsync($"/customer/v1/addresses/{address.Id.Value}", addrReq, ct);
            }
            else
            {
                // Create new
                await httpClient.PostAsJsonAsync("/customer/v1/addresses", addrReq, ct);
            }
        }

        return updatedCustomer;
    }

    /// <summary>
    /// Updates the status of an NDA record.
    /// </summary>
    public async Task<bool> UpdateNdaStatusAsync(Guid ndaId, string status, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/ndas/{ndaId}/status", new { status }, ct);
        return response.IsSuccessStatusCode;
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
                
                string message = "Company creation failed";
                try {
                    var apiErr = System.Text.Json.JsonSerializer.Deserialize<ApiErrorResponse>(errorBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (apiErr != null && !string.IsNullOrEmpty(apiErr.Message)) message = apiErr.Message;
                } catch {}
                
                throw new HttpRequestException(message, null, companyResponse.StatusCode);
            }
        }

        // 2. Create customer
        request.Customer.CompanyId = companyId;
        var customerResponse = await httpClient.PostAsJsonAsync("/customer/v1/customers", request.Customer, ct);
        if (!customerResponse.IsSuccessStatusCode)
        {
            var errorBody = await customerResponse.Content.ReadAsStringAsync(ct);
            logger.LogError("Customer creation failed ({StatusCode}): {ErrorBody}", customerResponse.StatusCode, errorBody);
            
            string message = "Customer creation failed";
            try {
                var apiErr = System.Text.Json.JsonSerializer.Deserialize<ApiErrorResponse>(errorBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiErr != null && !string.IsNullOrEmpty(apiErr.Message)) message = apiErr.Message;
            } catch {}

            throw new HttpRequestException(message, null, customerResponse.StatusCode);
        }
        
        var createdCustomer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>(ct);
        if (createdCustomer == null) return null;

        var newCustomerId = createdCustomer.Id;

        // 3. Create internal note if provided
        if (!string.IsNullOrWhiteSpace(request.InternalNote))
        {
            var noteReq = new
            {
                ownerType = "Customer",
                ownerId = newCustomerId,
                noteText = request.InternalNote
            };
            await httpClient.PostAsJsonAsync("/customer/v1/internal-notes", noteReq, ct);
        }

        // 4. Create addresses
        foreach (var address in request.Addresses)
        {
            // Use the microservice's expected polymorphic structure with the confirmed newCustomerId
            var response = await httpClient.PostAsJsonAsync("/customer/v1/addresses", new
            {
                ownerType = "Customer",
                ownerId = newCustomerId,
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

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Address creation failed for customer {CustomerId} ({StatusCode}): {ErrorBody}",
                    newCustomerId, response.StatusCode, errorBody);
                throw new HttpRequestException(ExtractErrorMessage(errorBody, "Address creation failed"), null, response.StatusCode);
            }
        }

        // 5. Create all documents (NDA and customer documents)
        if (request.Documents.Any())
        {
            var createdDocuments = await CreateDocumentsAsync(
                "Customer", newCustomerId, request.Documents, ct);

            // 5a. If NDA is active, link the signed document
            if (request.Nda != null && request.Nda.IsActive)
            {
                var signedNdaDoc = createdDocuments
                    .FirstOrDefault(d => d.DocumentCategory == "NDA" &&
                                        d.DocumentSubType == "Signed");

                var ndaReq = new
                {
                    customerId = newCustomerId,
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
    /// Creates company (if provided), customer, and internal note.
    /// Returns the created customer with ID.
    /// </summary>
    public async Task<CustomerResponse?> CreateBasicAsync(CustomerOnboardingRequest request, CancellationToken ct = default)
    {
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

                string message = "Company creation failed";
                try
                {
                    var apiErr = System.Text.Json.JsonSerializer.Deserialize<ApiErrorResponse>(errorBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (apiErr != null && !string.IsNullOrEmpty(apiErr.Message)) message = apiErr.Message;
                }
                catch { }

                throw new HttpRequestException(message, null, companyResponse.StatusCode);
            }
        }

        // 2. Create customer
        request.Customer.CompanyId = companyId;
        var customerResponse = await httpClient.PostAsJsonAsync("/customer/v1/customers", request.Customer, ct);
        if (!customerResponse.IsSuccessStatusCode)
        {
            var errorBody = await customerResponse.Content.ReadAsStringAsync(ct);
            logger.LogError("Customer creation failed ({StatusCode}): {ErrorBody}", customerResponse.StatusCode, errorBody);

            string message = "Customer creation failed";
            try
            {
                var apiErr = System.Text.Json.JsonSerializer.Deserialize<ApiErrorResponse>(errorBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiErr != null && !string.IsNullOrEmpty(apiErr.Message)) message = apiErr.Message;
            }
            catch { }

            throw new HttpRequestException(message, null, customerResponse.StatusCode);
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

        return createdCustomer;
    }

    /// <summary>
    /// Creates addresses for a customer.
    /// </summary>
    public async Task<List<AddressResponse>> CreateAddressesAsync(Guid customerId, List<CreateAddressRequest> addresses, CancellationToken ct = default)
    {
        var results = new List<AddressResponse>();

        foreach (var address in addresses)
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

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Address creation failed for customer {CustomerId} ({StatusCode}): {ErrorBody}",
                    customerId, response.StatusCode, errorBody);
                throw new HttpRequestException(ExtractErrorMessage(errorBody, "Address creation failed"), null, response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<AddressResponse>(ct);
            if (result != null) results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Creates NDA for a customer, linking documents if available.
    /// </summary>
    public async Task CreateNdaWithDocumentsAsync(Guid customerId, CreateNDARequest nda, List<DocumentResponse> documents, CancellationToken ct = default)
    {
        var signedNdaDoc = documents
            .FirstOrDefault(d => d.DocumentCategory == "NDA" && d.DocumentSubType == "Signed");

        var ndaReq = new
        {
            customerId,
            documentReferenceId = signedNdaDoc?.Id,
            expiresAt = nda.ExpiresAt
        };

        var ndaResponse = await httpClient.PostAsJsonAsync("/customer/v1/ndas", ndaReq, ct);
        if (!ndaResponse.IsSuccessStatusCode)
        {
            var errorBody = await ndaResponse.Content.ReadAsStringAsync(ct);
            logger.LogError("NDA creation failed for customer {CustomerId} ({StatusCode}): {ErrorBody}",
                customerId, ndaResponse.StatusCode, errorBody);
            throw new HttpRequestException(ExtractErrorMessage(errorBody, "NDA creation failed"), null, ndaResponse.StatusCode);
        }

        // Update status to Signed
        var ndaJson = await ndaResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(ct);
        if (ndaJson.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
            ndaJson.TryGetProperty("id", out var ndaIdProp) &&
            ndaJson.TryGetProperty("version", out var versionProp))
        {
            Guid ndaId = ndaIdProp.GetGuid();
            await httpClient.PatchAsJsonAsync(
                $"/customer/v1/ndas/{ndaId}/status",
                new { status = "Signed", version = versionProp.GetString() }, ct);
        }
    }

    /// <summary>
    /// Extracts a clean error message from a downstream API error response body.
    /// Falls back to the provided default message if parsing fails.
    /// </summary>
    private static string ExtractErrorMessage(string errorBody, string fallback)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(errorBody);
            if (json.TryGetProperty("error", out var errorProp) && errorProp.ValueKind == System.Text.Json.JsonValueKind.String)
                return errorProp.GetString() ?? fallback;
            if (json.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == System.Text.Json.JsonValueKind.String)
                return msgProp.GetString() ?? fallback;
            if (json.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == System.Text.Json.JsonValueKind.String)
                return titleProp.GetString() ?? fallback;
        }
        catch { }

        // If not JSON or no known field, return fallback
        return fallback;
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
                documentType = doc.DocumentCategory, // Corrected mapping
                fileReference = doc.FileReference,
                filename = doc.FileName // Corrected casing
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
    /// <param name="rowVersion">The document row version for optimistic concurrency.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> DeleteDocumentAsync(
        Guid documentId,
        byte[] rowVersion,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/documents/{documentId}")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { version = rowVersion })
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
    /// Retrieves a single company by ID.
    /// </summary>
    public async Task<CompanyResponse?> GetCompanyByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<CompanyResponse>($"/customer/v1/companies/{id}", ct);
    }

    /// <summary>
    /// Creates an NDA record for a customer.
    /// </summary>
    public async Task<bool> CreateNdaAsync(object ndaRequest, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/customer/v1/ndas", ndaRequest, ct);
        return response.IsSuccessStatusCode;
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

    /// <summary>
    /// Creates an internal note for a customer or company.
    /// </summary>
    public async Task<InternalNoteResponse?> CreateInternalNoteAsync(CreateInternalNoteRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("/customer/v1/internal-notes", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<InternalNoteResponse>(ct);
        }
        return null;
    }

    /// <summary>
    /// Updates an existing internal note.
    /// </summary>
    public async Task<InternalNoteResponse?> UpdateInternalNoteAsync(Guid id, UpdateInternalNoteRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"/customer/v1/internal-notes/{id}", request, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<InternalNoteResponse>(ct);
        }
        return null;
    }

    /// <summary>
    /// Deletes an internal note.
    /// </summary>
    public async Task<bool> DeleteInternalNoteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/customer/v1/internal-notes/{id}", ct);
        return response.IsSuccessStatusCode;
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

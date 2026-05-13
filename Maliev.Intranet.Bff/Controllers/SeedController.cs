using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Bff.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Net.Http.Json;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for database seeding operations. Internal use only.
/// Requires user authentication - uses the logged-in user's JWT token for API calls.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SeedController(
    IHttpClientFactory httpClientFactory,
    IHubContext<NotificationHub> hubContext,
    ILogger<SeedController> logger) : ControllerBase
{
    private Guid _thailandCountryId;

    /// <summary>
    /// Seeds the Maliev customer data. Idempotent - checks if data already exists.
    /// Requires user to be logged in with appropriate permissions.
    /// </summary>
    [RequirePermission(MalievPermissions.Customer.Write, AuthenticationSchemes = "Bearer,Cookies")]
    [HttpPost("customers")]
    [HttpPost("/api/seed/customers")]
    public async Task<IActionResult> SeedCustomers(CancellationToken ct)
    {
        logger.LogInformation("Starting customer data seeding...");

        // Use separate clients per service — service account auth, no UserContextHandler
        var customerClient = httpClientFactory.CreateClient("SeedCustomerClient");
        var countryClient = httpClientFactory.CreateClient("SeedCountryClient");

        // Look up Thailand's country ID first (requires country.countries.read)
        try
        {
            _thailandCountryId = await GetThailandCountryIdAsync(countryClient, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Missing required permission"))
        {
            return StatusCode(403, new ApiErrorResponse { Message = ex.Message });
        }

        if (_thailandCountryId == Guid.Empty)
        {
            return StatusCode(500, new ApiErrorResponse { Message = "Thailand country not found in CountryService." });
        }
        logger.LogInformation("Using Thailand country ID: {CountryId}", _thailandCountryId);

        try
        {
            var seedData = SeedCustomerDataFactory.CreateLocalTestingData();
            var existingCustomers = await GetCustomersAsync(customerClient, ct);
            var existingEmails = existingCustomers?.Items
                .Select(customer => customer.Email)
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
            var companiesByKey = new Dictionary<string, CompanyResponse>(StringComparer.OrdinalIgnoreCase);
            var createdCustomerCount = 0;
            var existingSeedCustomerCount = 0;
            var createdAddressCount = 0;
            var createdNoteCount = 0;

            foreach (var customerSeed in seedData.Customers)
            {
                if (existingEmails.Contains(customerSeed.Email))
                {
                    existingSeedCustomerCount++;
                    continue;
                }

                Guid? companyId = null;
                if (customerSeed.CompanyKey is not null)
                {
                    var companySeed = seedData.Companies.First(company => company.Key == customerSeed.CompanyKey);
                    var company = await GetOrCreateCompanyAsync(customerClient, companySeed, companiesByKey, ct);
                    if (company == null)
                    {
                        return StatusCode(500, new ApiErrorResponse { Message = $"Failed to create company '{companySeed.Name}'." });
                    }

                    companyId = company.Id;
                }

                var customer = await CreateCustomerAsync(customerClient, customerSeed, companyId, ct);
                if (customer == null)
                {
                    return StatusCode(500, new ApiErrorResponse { Message = $"Failed to create customer '{customerSeed.Email}'." });
                }

                createdCustomerCount++;

                foreach (var addressSeed in customerSeed.Addresses)
                {
                    var address = await CreateCustomerAddressAsync(customerClient, customer.Id, addressSeed, ct);
                    if (address != null)
                    {
                        createdAddressCount++;
                    }
                }

                if (!string.IsNullOrWhiteSpace(customerSeed.InternalNote))
                {
                    var note = await CreateInternalNoteAsync(customerClient, "Customer", customer.Id, customerSeed.InternalNote, ct);
                    if (note != null)
                    {
                        createdNoteCount++;
                    }
                }
            }

            logger.LogInformation(
                "Customer seed completed. Created {CreatedCustomers} customers, reused {ExistingCustomers} existing seed customers, created {CreatedAddresses} addresses and {CreatedNotes} notes.",
                createdCustomerCount,
                existingSeedCustomerCount,
                createdAddressCount,
                createdNoteCount);
            await hubContext.Clients.All.SendAsync("CustomerChanged", cancellationToken: ct);

            return Ok(new MalievResponse<object>
            {
                Success = true,
                Message = $"Customer seed completed. {createdCustomerCount} customers created, {existingSeedCustomerCount} seed customers already existed.",
                Data = new
                {
                    requestedCustomerCount = seedData.Customers.Count,
                    createdCustomerCount,
                    existingSeedCustomerCount,
                    createdCompanyCount = companiesByKey.Count,
                    createdAddressCount,
                    createdNoteCount,
                    documentReferenceCount = seedData.DocumentReferences.Count,
                    ndaRecordCount = seedData.NdaRecords.Count
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed customer data.");
            return StatusCode(500, new ApiErrorResponse { Message = $"Failed to seed customer data: {ex.Message}" });
        }
    }

    private async Task<CustomerPaginatedResponse?> GetCustomersAsync(HttpClient client, CancellationToken ct)
    {
        return await client.GetFromJsonAsync<CustomerPaginatedResponse>(
            "/customer/v1/customers?page=1&pageSize=200&sortBy=createdAt&sortDirection=desc", ct);
    }

    private async Task<CompanyResponse?> GetOrCreateCompanyAsync(
        HttpClient client,
        SeedCompanyDefinition companySeed,
        Dictionary<string, CompanyResponse> companiesByKey,
        CancellationToken ct)
    {
        if (companiesByKey.TryGetValue(companySeed.Key, out var existingCompany))
        {
            return existingCompany;
        }

        var company = await CreateCompanyAsync(client, companySeed, ct);
        if (company != null)
        {
            companiesByKey[companySeed.Key] = company;
        }

        return company;
    }

    private async Task<CompanyResponse?> CreateCompanyAsync(
        HttpClient client,
        SeedCompanyDefinition companySeed,
        CancellationToken ct)
    {
        var companyRequest = new
        {
            name = companySeed.Name,
            vatNumber = companySeed.VatNumber,
            registrationNumber = companySeed.RegistrationNumber,
            contactPhone = companySeed.ContactPhone,
            segment = companySeed.Segment,
            tier = companySeed.Tier,
            isVerifiedFromBdex = false
        };

        var companyResponse = await client.PostAsJsonAsync("/customer/v1/companies", companyRequest, ct);
        if (companyResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            companyResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Missing required permission: customer.companies.manage. " +
                "Please log in as a user with Platform Owner role.");
        }

        CompanyResponse? company = null;
        if (companyResponse.IsSuccessStatusCode)
        {
            company = await companyResponse.Content.ReadFromJsonAsync<CompanyResponse>(ct);
        }

        if (company == null)
        {
            // Company might already exist — search for it
            var searchResponse = await client.GetFromJsonAsync<CustomerPaginatedCompanyResponse>(
                $"/customer/v1/companies?query={Uri.EscapeDataString(companySeed.Name)}", ct);
            company = searchResponse?.Items.FirstOrDefault(c => c.VatNumber == companySeed.VatNumber);
        }

        if (company != null)
        {
            logger.LogInformation("Created/found company: {CompanyId} - {Name}", company.Id, company.Name);
        }

        return company;
    }

    private async Task<CustomerResponse?> CreateCustomerAsync(
        HttpClient client,
        SeedCustomerDefinition customerSeed,
        Guid? companyId,
        CancellationToken ct)
    {
        var request = new
        {
            firstName = customerSeed.FirstName,
            lastName = customerSeed.LastName,
            email = customerSeed.Email,
            mobile = customerSeed.Mobile,
            landline = customerSeed.Landline,
            extension = customerSeed.Extension,
            segment = customerSeed.Segment,
            tier = customerSeed.Tier,
            preferredLanguage = customerSeed.PreferredLanguage,
            timezone = customerSeed.Timezone,
            companyId,
            usesCompanyBillingAddress = customerSeed.UsesCompanyBillingAddress,
            communicationPreferences = customerSeed.CommunicationPreferences
        };

        var response = await client.PostAsJsonAsync("/customer/v1/customers", request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Missing required permission: customer.customers.create. " +
                "Please log in as a user with Platform Owner role.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Failed to create customer: {StatusCode} - {Error}", response.StatusCode, error);
            return null;
        }

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>(ct);
        if (customer != null)
        {
            logger.LogInformation("Created customer: {CustomerId} - {FirstName} {LastName}",
                customer.Id, customer.FirstName, customer.LastName);
        }
        return customer;
    }

    private async Task<AddressResponse?> CreateCustomerAddressAsync(
        HttpClient client,
        Guid customerId,
        SeedAddressDefinition addressSeed,
        CancellationToken ct)
    {
        var addressRequest = new
        {
            ownerType = "Customer",
            ownerId = customerId,
            type = addressSeed.Type,
            isDefault = addressSeed.IsDefault,
            addressLine1 = addressSeed.AddressLine1,
            addressLine2 = addressSeed.AddressLine2,
            district = addressSeed.District,
            city = addressSeed.City,
            stateProvince = addressSeed.StateProvince,
            postalCode = addressSeed.PostalCode,
            countryId = _thailandCountryId,
            recipientName = addressSeed.RecipientName,
            recipientPhone = addressSeed.RecipientPhone
        };

        var response = await client.PostAsJsonAsync("/customer/v1/addresses", addressRequest, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Missing required permission: customer.addresses.manage. " +
                "Please log in as a user with Platform Owner role.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Failed to create customer address: {StatusCode} - {Error}", response.StatusCode, error);
            return null;
        }

        var address = await response.Content.ReadFromJsonAsync<AddressResponse>(ct);
        if (address != null)
        {
            logger.LogInformation("Created customer address: {AddressId}", address.Id);
        }
        return address;
    }

    private async Task<InternalNoteResponse?> CreateInternalNoteAsync(HttpClient client, string ownerType, Guid ownerId, string noteText, CancellationToken ct)
    {
        var noteRequest = new
        {
            ownerType,
            ownerId,
            noteText
        };

        var response = await client.PostAsJsonAsync("/customer/v1/internal-notes", noteRequest, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                "Missing required permission: customer.notes.create. " +
                "Please log in as a user with Platform Owner role.");
        }

        if (!response.IsSuccessStatusCode) return null;

        var note = await response.Content.ReadFromJsonAsync<InternalNoteResponse>(ct);
        if (note != null)
        {
            logger.LogInformation("Created internal note: {NoteId} for {OwnerType} {OwnerId}", note.Id, ownerType, ownerId);
        }
        return note;
    }

    // Private DTOs for deserialization — only what the seeder needs
    private class CustomerPaginatedResponse
    {
        public List<CustomerSummaryItem> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }

    private class CustomerPaginatedCompanyResponse
    {
        public List<CompanyResponse> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }

    private class CustomerSummaryItem
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    private class CompanyResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string VatNumber { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Segment { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
    }

    private class CustomerResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private class AddressResponse
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    private class InternalNoteResponse
    {
        public Guid Id { get; set; }
        public string NoteText { get; set; } = string.Empty;
    }

    private class CountryResponse
    {
        public Guid Id { get; set; }
        public string Iso2 { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private async Task<Guid> GetThailandCountryIdAsync(HttpClient client, CancellationToken ct)
    {
        try
        {
            // Use /iso2/{iso2} endpoint - returns single country by ISO code
            var response = await client.GetAsync("/country/v1/countries/iso2/TH", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException(
                    "Missing required permission: country.countries.read. " +
                    "Please log in as a user with Platform Owner role.");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to fetch Thailand by ISO2: {StatusCode}", response.StatusCode);
                return Guid.Empty;
            }

            var thailand = await response.Content.ReadFromJsonAsync<CountryResponse>(ct);
            if (thailand == null)
            {
                logger.LogWarning("Thailand country not found in CountryService");
                return Guid.Empty;
            }

            return thailand.Id;
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw permission errors
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching Thailand country ID");
            return Guid.Empty;
        }
    }

    private class CountryListResponse
    {
        public List<CountryResponse> Items { get; set; } = [];
    }
}

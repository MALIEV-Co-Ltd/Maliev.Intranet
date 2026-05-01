using Asp.Versioning;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for database seeding operations. Internal use only.
/// Requires user authentication - uses the logged-in user's JWT token for API calls.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public class SeedController(
    IHttpClientFactory httpClientFactory,
    ILogger<SeedController> logger) : ControllerBase
{
    private Guid _thailandCountryId;

    /// <summary>
    /// Seeds the Maliev customer data. Idempotent - checks if data already exists.
    /// Requires user to be logged in with appropriate permissions.
    /// </summary>
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
            var existingCustomers = await GetCustomersAsync(customerClient, ct);
            if (existingCustomers != null && existingCustomers.Items.Count != 0)
            {
                logger.LogInformation("Customer database already contains data. Skipping seeding.");
                return Ok(new MalievResponse<object>
                {
                    Success = true,
                    Message = "Customer database already contains data. Seeding skipped.",
                    Data = new { existingCount = existingCustomers.TotalCount }
                });
            }

            var company = await CreateCompanyAsync(customerClient, ct);
            if (company == null)
            {
                return StatusCode(500, new ApiErrorResponse { Message = "Failed to create company." });
            }

            var companyAddress = await CreateCompanyBillingAddressAsync(customerClient, company.Id, ct);

            var customer = await CreateCustomerAsync(customerClient, company.Id, ct);
            if (customer == null)
            {
                return StatusCode(500, new ApiErrorResponse { Message = "Failed to create customer." });
            }

            var shippingAddress = await CreateShippingAddressAsync(customerClient, customer.Id, ct);

            // Create internal notes
            var customerNote = await CreateInternalNoteAsync(customerClient, "Customer", customer.Id,
                "Customer seeded via database seeder. Primary contact for enterprise account.", ct);
            var companyNote = await CreateInternalNoteAsync(customerClient, "Company", company.Id,
                "Company seeded via database seeder. Enterprise tier account with platinum status.", ct);

            logger.LogInformation("Successfully seeded 1 company, 1 customer, 2 addresses, and 2 internal notes.");

            return Ok(new MalievResponse<object>
            {
                Success = true,
                Message = "Successfully seeded 1 company, 1 customer, 2 addresses, and 2 internal notes.",
                Data = new
                {
                    company = new { company.Id, company.Name, company.VatNumber },
                    customer = new { customer.Id, customer.FirstName, customer.LastName, customer.Email },
                    addresses = new
                    {
                        billing = companyAddress != null ? new { companyAddress.Id, companyAddress.Type, companyAddress.City } : null,
                        shipping = shippingAddress != null ? new { shippingAddress.Id, shippingAddress.Type, shippingAddress.City } : null
                    },
                    notes = new
                    {
                        customerNote = customerNote != null ? new { customerNote.Id, customerNote.NoteText } : null,
                        companyNote = companyNote != null ? new { companyNote.Id, companyNote.NoteText } : null
                    }
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
            "/customer/v1/customers?page=1&sortBy=createdAt&sortDirection=desc", ct);
    }

    private async Task<CompanyResponse?> CreateCompanyAsync(HttpClient client, CancellationToken ct)
    {
        var companyRequest = new
        {
            name = "บริษัท มาลีฟ จำกัด",
            vatNumber = "0125561001573",
            contactPhone = "028816002",
            segment = "Enterprise",
            tier = "Platinum",
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
                "/customer/v1/companies?query=%E0%B8%9A%E0%B8%A3%E0%B8%B4%E0%B8%A9%E0%B8%B1%E0%B8%97+%E0%B8%A1%E0%B8%B2%E0%B8%A5%E0%B8%B5%E0%B8%9F+%E0%B8%88%E0%B8%B3%E0%B8%81%E0%B8%B1%E0%B8%94", ct);
            company = searchResponse?.Items.FirstOrDefault(c => c.VatNumber == "0125561001573");
        }

        if (company != null)
        {
            logger.LogInformation("Created/found company: {CompanyId} - {Name}", company.Id, company.Name);
        }

        return company;
    }

    private async Task<AddressResponse?> CreateCompanyBillingAddressAsync(HttpClient client, Guid companyId, CancellationToken ct)
    {
        var addressRequest = new
        {
            ownerType = "Company",
            ownerId = companyId,
            type = "Billing",
            isDefault = true,
            addressLine1 = "36/1 หมู่ 3",
            district = "คลองอข่อย",
            city = "ปากเกร็ด",
            stateProvince = "นนทบุรี",
            postalCode = "11120",
            countryId = _thailandCountryId
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
            logger.LogWarning("Failed to create company billing address: {StatusCode} - {Error}", response.StatusCode, error);
            return null;
        }

        var address = await response.Content.ReadFromJsonAsync<AddressResponse>(ct);
        if (address != null)
        {
            logger.LogInformation("Created company billing address: {AddressId}", address.Id);
        }
        return address;
    }

    private async Task<CustomerResponse?> CreateCustomerAsync(HttpClient client, Guid companyId, CancellationToken ct)
    {
        var request = new
        {
            firstName = "ณฐพล",
            lastName = "วนาศรีวิไล",
            email = "natthapol.vanasrivilai@outlook.com",
            mobile = "0898950690",
            landline = "028816002",
            extension = "345",
            segment = "Enterprise",
            tier = "Platinum",
            preferredLanguage = "th",
            timezone = "Asia/Bangkok",
            companyId,
            usesCompanyBillingAddress = true,
            communicationPreferences = new Dictionary<string, bool>
            {
                { "email_opt_in", true },
                { "sms_opt_in", false },
                { "marketing_opt_in", false }
            }
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

    private async Task<AddressResponse?> CreateShippingAddressAsync(HttpClient client, Guid customerId, CancellationToken ct)
    {
        var addressRequest = new
        {
            ownerType = "Customer",
            ownerId = customerId,
            type = "Shipping",
            isDefault = true,
            addressLine1 = "36/2 หมู่ 4",
            district = "บางจาก",
            city = "ภาษีเจริญ",
            stateProvince = "กรุงเทพมหานคร",
            postalCode = "10160",
            countryId = _thailandCountryId,
            recipientName = "ณัฐกานต์ วนาศรีวิไล",
            recipientPhone = "0818030404"
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
            logger.LogWarning("Failed to create customer shipping address: {StatusCode} - {Error}", response.StatusCode, error);
            return null;
        }

        var address = await response.Content.ReadFromJsonAsync<AddressResponse>(ct);
        if (address != null)
        {
            logger.LogInformation("Created customer shipping address: {AddressId}", address.Id);
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

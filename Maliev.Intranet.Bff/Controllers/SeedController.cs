using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Maliev.Intranet.Bff.Controllers;

/// <summary>
/// Controller for database seeding operations. Internal use only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SeedController(
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory) : ControllerBase
{
    private ILogger<SeedController> logger => loggerFactory.CreateLogger<SeedController>();
    private CustomerServiceClient customerClient => new(httpClientFactory.CreateClient("CustomerServiceSeed"), loggerFactory.CreateLogger<CustomerServiceClient>());

    private record CountryLookupResult { public Guid Id { get; init; } }

    /// <summary>
    /// Seeds the Maliev customer data. Idempotent - checks if data already exists.
    /// </summary>
    [HttpPost("customers")]
    public async Task<IActionResult> SeedCustomers(CancellationToken ct)
    {
        logger.LogInformation("Starting customer data seeding...");

        try
        {
            var existingCustomers = await customerClient.GetCustomersAsync(page: 1, ct: ct);
            if (existingCustomers != null && existingCustomers.Data.Any())
            {
                logger.LogInformation("Customer database already contains data. Skipping seeding.");
                return Ok(new MalievResponse<object>
                {
                    Success = true,
                    Message = "Customer database already contains data. Seeding skipped.",
                    Data = new { existingCount = existingCustomers.Meta.TotalCount }
                });
            }

            var countryClient = httpClientFactory.CreateClient("CountryServiceAccount");
            var thailand = await countryClient.GetFromJsonAsync<CountryLookupResult>("/country/v1/countries/iso2/TH", ct);
            if (thailand == null)
            {
                return StatusCode(500, new ApiErrorResponse { Message = "Failed to look up Thailand country ID." });
            }

            var company = await CreateCompanyAsync(ct);
            if (company == null)
            {
                return StatusCode(500, new ApiErrorResponse { Message = "Failed to create company." });
            }

            var companyAddress = await CreateCompanyBillingAddressAsync(company.Id, thailand.Id, ct);

            var customer = await CreateCustomerAsync(company.Id, ct);
            if (customer == null)
            {
                return StatusCode(500, new ApiErrorResponse { Message = "Failed to create customer." });
            }

            var shippingAddress = await CreateShippingAddressAsync(customer.Id, thailand.Id, ct);
            var customerBillingAddress = await CreateCustomerBillingAddressAsync(customer.Id, thailand.Id, ct);
            await SeedInternalNotesAsync(customer.Id, ct);

            logger.LogInformation("Successfully seeded 1 company, 1 customer, addresses, and internal notes.");

            return Ok(new MalievResponse<object>
            {
                Success = true,
                Message = "Successfully seeded 1 company, 1 customer, addresses, and internal notes.",
                Data = new
                {
                    company = new { company.Id, company.Name, company.VatNumber },
                    customer = new { customer.Id, customer.FirstName, customer.LastName, customer.Email },
                    addresses = new
                    {
                        companyBilling = companyAddress != null ? new { companyAddress.Id, companyAddress.Type, companyAddress.City } : null,
                        customerBilling = customerBillingAddress != null ? new { customerBillingAddress.Id, customerBillingAddress.Type, customerBillingAddress.City } : null,
                        shipping = shippingAddress != null ? new { shippingAddress.Id, shippingAddress.Type, shippingAddress.City } : null
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

    private async Task<CompanyResponse?> CreateCompanyAsync(CancellationToken ct)
    {
        var httpClient = httpClientFactory.CreateClient("CustomerServiceSeed");

        var companyResponse = await httpClient.PostAsJsonAsync("/customer/v1/companies", new CreateCompanyRequest
        {
            Name = "บริษัท มาลีฟ จำกัด",
            VatNumber = "0125561001573",
            ContactPhone = "028816002",
            Segment = "Enterprise",
            Tier = "Platinum",
            IsVerifiedFromBdex = false
        }, ct);

        if (!companyResponse.IsSuccessStatusCode)
        {
            logger.LogError("Failed to create company. Status: {StatusCode}", companyResponse.StatusCode);
            return null;
        }

        var company = await companyResponse.Content.ReadFromJsonAsync<CompanyResponse>(ct);
        if (company == null) return null;

        logger.LogInformation("Created company: {CompanyId} - {Name}", company.Id, company.Name);
        return company;
    }

    private async Task<AddressResponse?> CreateCompanyBillingAddressAsync(Guid companyId, Guid countryId, CancellationToken ct)
    {
        var httpClient = httpClientFactory.CreateClient("CustomerServiceSeed");

        var response = await httpClient.PostAsJsonAsync("/customer/v1/addresses", new
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
            countryId
        }, ct);

        if (!response.IsSuccessStatusCode) return null;

        var address = await response.Content.ReadFromJsonAsync<AddressResponse>(ct);
        if (address != null)
            logger.LogInformation("Created company billing address: {AddressId}", address.Id);

        return address;
    }

    private async Task<CustomerResponse?> CreateCustomerAsync(Guid companyId, CancellationToken ct)
    {
        var request = new CreateCustomerRequest
        {
            FirstName = "ณฐพล",
            LastName = "วนาศรีวิไล",
            Email = "natthapol.vanasrivilai@outlook.com",
            Mobile = "0898950690",
            Landline = "028816002",
            Extension = "345",
            Segment = "Enterprise",
            Tier = "Platinum",
            PreferredLanguage = "th",
            Timezone = "Asia/Bangkok",
            CompanyId = companyId,
            UsesCompanyBillingAddress = true,
            CommunicationPreferences = new Dictionary<string, bool>
            {
                { "email_opt_in", true },
                { "sms_opt_in", false },
                { "marketing_opt_in", false }
            }
        };

        var result = await customerClient.CreateCustomerAsync(request, ct);

        if (result != null)
        {
            logger.LogInformation("Created customer: {CustomerId} - {FirstName} {LastName}",
                result.Id, result.FirstName, result.LastName);
        }

        return result;
    }

    private async Task<AddressResponse?> CreateCustomerBillingAddressAsync(Guid customerId, Guid countryId, CancellationToken ct)
    {
        var addresses = new List<CreateAddressRequest>
        {
            new()
            {
                Type = "Billing",
                IsDefault = true,
                AddressLine1 = "36/1 หมู่ 3",
                District = "คลองอข่อย",
                City = "ปากเกร็ด",
                StateProvince = "นนทบุรี",
                PostalCode = "11120",
                CountryId = countryId
            }
        };

        var result = await customerClient.CreateAddressesAsync(customerId, addresses, ct);
        var address = result.FirstOrDefault();

        if (address != null)
            logger.LogInformation("Created customer billing address: {AddressId}", address.Id);

        return address;
    }

    private async Task SeedInternalNotesAsync(Guid customerId, CancellationToken ct)
    {
        await customerClient.AddInternalNoteAsync(customerId, new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId,
            NoteText = "ลูกค้าระดับ Platinum ของ Maliev — ติดต่อผ่านอีเมลเป็นหลัก ไม่รับสาย SMS"
        }, ct);

        await customerClient.AddInternalNoteAsync(customerId, new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId,
            NoteText = "ใบเสนอราคาและเอกสารสัญญาส่งผ่านระบบ e-Document เท่านั้น กรุณาอย่าส่งเป็น hardcopy"
        }, ct);

        logger.LogInformation("Seeded 2 internal notes for customer {CustomerId}", customerId);
    }

    private async Task<AddressResponse?> CreateShippingAddressAsync(Guid customerId, Guid countryId, CancellationToken ct)
    {
        var addresses = new List<CreateAddressRequest>
        {
            new()
            {
                Type = "Shipping",
                IsDefault = true,
                AddressLine1 = "36/2 หมู่ 4",
                District = "บางจาก",
                City = "ภาษีเจริญ",
                StateProvince = "กรุงเทพมหานคร",
                PostalCode = "10160",
                CountryId = countryId,
                RecipientName = "ณัฐกานต์ วนาศรีวิไล",
                RecipientPhone = "0818030404"
            }
        };

        var result = await customerClient.CreateAddressesAsync(customerId, addresses, ct);
        var address = result.FirstOrDefault();

        if (address != null)
        {
            logger.LogInformation("Created customer shipping address: {AddressId}", address.Id);
        }

        return address;
    }
}

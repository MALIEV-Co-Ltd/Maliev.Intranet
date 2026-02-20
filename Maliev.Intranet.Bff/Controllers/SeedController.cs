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
    CustomerServiceClient customerClient,
    ILogger<SeedController> logger) : ControllerBase
{
    private static readonly Guid ThailandCountryId = Guid.Parse("00000000-0000-0000-0000-000000000001");

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

            var company = await CreateCompanyAsync(ct);
            if (company == null)
            {
                return StatusCode(500, new ApiErrorResponse { Message = "Failed to create company." });
            }

            var companyAddress = await CreateCompanyBillingAddressAsync(company.Id, ct);

            var customer = await CreateCustomerAsync(company.Id, ct);
            if (customer == null)
            {
                return StatusCode(500, new ApiErrorResponse { Message = "Failed to create customer." });
            }

            var shippingAddress = await CreateShippingAddressAsync(customer.Id, ct);

            logger.LogInformation("Successfully seeded 1 company, 1 customer, and 2 addresses.");

            return Ok(new MalievResponse<object>
            {
                Success = true,
                Message = "Successfully seeded 1 company, 1 customer, and 2 addresses.",
                Data = new
                {
                    company = new { company.Id, company.Name, company.VatNumber },
                    customer = new { customer.Id, customer.FirstName, customer.LastName, customer.Email },
                    addresses = new
                    {
                        billing = companyAddress != null ? new { companyAddress.Id, companyAddress.Type, companyAddress.City } : null,
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
        var request = new CustomerOnboardingRequest
        {
            NewCompany = new CreateCompanyRequest
            {
                Name = "บริษัท มาลีฟ จำกัด",
                VatNumber = "0125561001573",
                ContactPhone = "028816002",
                Segment = "Enterprise",
                Tier = "Platinum",
                IsVerifiedFromBdex = false
            },
            Customer = new CreateCustomerRequest
            {
                FirstName = "PLACEHOLDER",
                LastName = "PLACEHOLDER",
                Email = "placeholder@maliev.internal",
                Segment = "Enterprise",
                Tier = "Platinum"
            }
        };

        var result = await customerClient.CreateCustomerBasicAsync(request, ct);

        if (result == null)
        {
            logger.LogError("Failed to create company via CreateCustomerBasicAsync.");
            return null;
        }

        var companies = await customerClient.GetCompaniesAsync(query: "บริษัท มาลีฟ จำกัด", ct: ct);
        var company = companies?.Data.FirstOrDefault(c => c.VatNumber == "0125561001573");

        if (company != null)
        {
            logger.LogInformation("Created company: {CompanyId} - {Name}", company.Id, company.Name);
            return new CompanyResponse
            {
                Id = company.Id,
                Name = company.Name,
                VatNumber = company.VatNumber,
                ContactPhone = company.ContactPhone,
                Segment = company.Segment,
                Tier = company.Tier
            };
        }

        return null;
    }

    private async Task<AddressResponse?> CreateCompanyBillingAddressAsync(Guid companyId, CancellationToken ct)
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
                CountryId = ThailandCountryId
            }
        };

        var result = await customerClient.CreateAddressesAsync(companyId, addresses, ct);
        var address = result.FirstOrDefault();

        if (address != null)
        {
            logger.LogInformation("Created company billing address: {AddressId}", address.Id);
        }

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

    private async Task<AddressResponse?> CreateShippingAddressAsync(Guid customerId, CancellationToken ct)
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
                CountryId = ThailandCountryId,
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

namespace Maliev.Intranet.Bff.Controllers;

internal static class SeedCustomerDataFactory
{
    private static readonly string[] FirstNames =
    [
        "ณฐพล", "Ananya", "Krit", "Sasithorn", "Thanawat",
        "Pimchanok", "Arthit", "Mayuree", "Somchai", "Kanya"
    ];

    private static readonly string[] LastNames =
    [
        "วนาศรีวิไล", "Sukhum", "Tanaka", "Chen", "Williams",
        "Garcia", "Kowalski", "Nguyen", "Patel", "Larsson"
    ];

    private static readonly string[] Segments =
    [
        "Retail",
        "Wholesale",
        "Enterprise",
        "Government"
    ];

    private static readonly string[] CustomerTiers =
    [
        "Bronze",
        "Silver",
        "Gold",
        "Platinum",
        "VIP"
    ];

    private static readonly string[] CompanyTiers =
    [
        "Classic",
        "Silver",
        "Gold"
    ];

    private static readonly string[] CompanyNames =
    [
        "Bangkok Precision Parts",
        "Chiang Mai Robotics",
        "Eastern Seaboard Tooling",
        "Global Fixture Labs",
        "Nordic Manufacturing Studio",
        "Pacific Aerospace Components",
        "Siam Medical Devices",
        "Urban Mobility Fabrication",
        "Vertex Defense Systems",
        "Wanasrivwilai Engineering"
    ];

    public static SeedCustomerDataSet CreateLocalTestingData()
    {
        var companies = CompanyNames
            .Select((name, index) => new SeedCompanyDefinition(
                Key: $"company-{index + 1:00}",
                Name: name,
                VatNumber: $"TH-SEED-{index + 1:0000000000}",
                RegistrationNumber: $"SEED-COMPANY-{index + 1:000}",
                ContactPhone: $"+662100{index:0000}",
                Segment: Segments[index % Segments.Length],
                Tier: CompanyTiers[index % CompanyTiers.Length]))
            .ToArray();

        var customers = Enumerable.Range(0, 50)
            .Select(index => CreateCustomer(index, companies))
            .ToArray();

        return new SeedCustomerDataSet(companies, customers, [], []);
    }

    private static SeedCustomerDefinition CreateCustomer(int index, IReadOnlyList<SeedCompanyDefinition> companies)
    {
        var company = index % 2 == 0 ? companies[index / 5 % companies.Count] : null;
        var email = index == 0
            ? "natthapol.vanasrivilai@outlook.com"
            : $"seed.customer{index + 1:00}@seed.maliev.local";
        var firstName = FirstNames[index % FirstNames.Length];
        var lastName = LastNames[index % LastNames.Length];
        var customerId = $"customer-{index + 1:00}";
        var addressCount = index % 3 == 0 ? 2 : 1;

        return new SeedCustomerDefinition(
            Key: customerId,
            FirstName: firstName,
            LastName: lastName,
            Email: email,
            Mobile: index == 0 ? "0898950690" : $"+6681{1000000 + index:0000000}",
            Landline: company is null ? null : $"+662555{index:0000}",
            Extension: company is null ? null : $"{100 + index}",
            Segment: Segments[index % Segments.Length],
            Tier: CustomerTiers[index % CustomerTiers.Length],
            PreferredLanguage: index % 3 == 0 ? "th" : "en",
            Timezone: "Asia/Bangkok",
            CompanyKey: company?.Key,
            UsesCompanyBillingAddress: company is not null && index % 4 == 0,
            CommunicationPreferences: new Dictionary<string, bool>
            {
                ["email_opt_in"] = true,
                ["sms_opt_in"] = false,
                ["marketing_opt_in"] = index % 5 == 0
            },
            Addresses: CreateAddresses(customerId, index, addressCount),
            InternalNote: index % 3 == 1 ? $"Local testing note for seed customer {index + 1:00}." : null);
    }

    private static IReadOnlyList<SeedAddressDefinition> CreateAddresses(string customerKey, int index, int addressCount)
    {
        var addresses = new List<SeedAddressDefinition>(addressCount)
        {
            CreateAddress(customerKey, index, "Billing")
        };

        if (addressCount > 1)
        {
            addresses.Add(CreateAddress(customerKey, index + 100, "Shipping"));
        }

        return addresses;
    }

    private static SeedAddressDefinition CreateAddress(string customerKey, int index, string type)
    {
        return new SeedAddressDefinition(
            CustomerKey: customerKey,
            Type: type,
            IsDefault: true,
            AddressLine1: $"{100 + index} Seed Testing Road",
            AddressLine2: index % 2 == 0 ? $"Building {index % 9 + 1}" : null,
            District: index % 2 == 0 ? "Khlong Toei" : "Mueang",
            City: index % 4 == 0 ? "Bangkok" : "Chiang Mai",
            StateProvince: index % 4 == 0 ? "Bangkok" : "Chiang Mai",
            PostalCode: index % 4 == 0 ? "10110" : "50000",
            RecipientName: $"Seed Customer {index + 1:00}",
            RecipientPhone: $"+6682{1000000 + index:0000000}");
    }
}

internal sealed record SeedCustomerDataSet(
    IReadOnlyList<SeedCompanyDefinition> Companies,
    IReadOnlyList<SeedCustomerDefinition> Customers,
    IReadOnlyList<object> DocumentReferences,
    IReadOnlyList<object> NdaRecords);

internal sealed record SeedCompanyDefinition(
    string Key,
    string Name,
    string VatNumber,
    string RegistrationNumber,
    string ContactPhone,
    string Segment,
    string Tier);

internal sealed record SeedCustomerDefinition(
    string Key,
    string FirstName,
    string LastName,
    string Email,
    string Mobile,
    string? Landline,
    string? Extension,
    string Segment,
    string Tier,
    string PreferredLanguage,
    string Timezone,
    string? CompanyKey,
    bool UsesCompanyBillingAddress,
    IReadOnlyDictionary<string, bool> CommunicationPreferences,
    IReadOnlyList<SeedAddressDefinition> Addresses,
    string? InternalNote)
{
    public int AddressCount => Addresses.Count;
}

internal sealed record SeedAddressDefinition(
    string CustomerKey,
    string Type,
    bool IsDefault,
    string AddressLine1,
    string? AddressLine2,
    string District,
    string City,
    string StateProvince,
    string PostalCode,
    string RecipientName,
    string RecipientPhone);

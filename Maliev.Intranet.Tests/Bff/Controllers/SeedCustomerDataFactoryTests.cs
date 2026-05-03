using Maliev.Intranet.Bff.Controllers;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class SeedCustomerDataFactoryTests
{
    [Fact]
    public void CreateLocalTestingData_DefaultDataset_ContainsFiftyCustomersWithRequestedVariations()
    {
        var seedData = SeedCustomerDataFactory.CreateLocalTestingData();

        Assert.Equal(50, seedData.Customers.Count);
        Assert.Contains(seedData.Customers, customer => customer.CompanyKey is null);
        Assert.Contains(seedData.Customers, customer => customer.CompanyKey is not null);
        Assert.Contains(seedData.Customers, customer => customer.AddressCount == 1);
        Assert.Contains(seedData.Customers, customer => customer.AddressCount > 1);
        Assert.Contains(seedData.Customers, customer => customer.InternalNote is not null);
        Assert.Contains(seedData.Customers, customer => customer.InternalNote is null);
        Assert.True(new[] { "Bronze", "Silver", "Gold", "Platinum", "VIP" }
            .All(tier => seedData.Customers.Any(customer => customer.Tier == tier)));
        Assert.Empty(seedData.DocumentReferences);
        Assert.Empty(seedData.NdaRecords);
    }
}

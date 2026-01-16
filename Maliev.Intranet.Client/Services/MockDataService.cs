using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Client.Services;

public class MockDataService
{
    public List<CustomerSummaryDto> GetCustomers()
    {
        return new List<CustomerSummaryDto>
        {
            new CustomerSummaryDto { Id = Guid.NewGuid(), Name = "Maliev Tech", Email = "contact@maliev.com", Status = "Active", OutstandingBalance = 0 },
            new CustomerSummaryDto { Id = Guid.NewGuid(), Name = "Acme Corp", Email = "info@acme.com", Status = "Inactive", OutstandingBalance = 1500.50m }
        };
    }

    public List<OrderSummaryDto> GetOrders()
    {
        return new List<OrderSummaryDto>
        {
            new OrderSummaryDto { Id = Guid.NewGuid(), OrderNumber = "ORD-001", CustomerName = "Maliev Tech", TotalAmount = 5000, Status = "Completed", CreatedAt = DateTime.Now.AddDays(-5) },
            new OrderSummaryDto { Id = Guid.NewGuid(), OrderNumber = "ORD-002", CustomerName = "Acme Corp", TotalAmount = 1200, Status = "Pending", CreatedAt = DateTime.Now.AddDays(-1) }
        };
    }
}

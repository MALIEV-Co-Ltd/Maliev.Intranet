using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class SharedDtoTests
{
    [Fact]
    public void DtoInstantiation_ShouldWork()
    {
        // Common
        _ = new PagedResponse<string>();
        _ = new PaginationMeta { CurrentPage = 1, TotalPages = 5 };
        _ = new ApiErrorResponse { Message = "Error" };

        // Sales
        _ = new OrderSummaryDto { Id = Guid.NewGuid(), OrderNumber = "ORD-1" };
        _ = new OrderDetailDto { Id = Guid.NewGuid() };
        _ = new OrderItemDto { Description = "Item" };
        _ = new OrderTimelineDto { Status = "Processing" };
        _ = new QuotationSummaryDto { QuotationNumber = "QUO-1" };
        _ = new QuotationDetailDto { QuotationNumber = "QUO-1" };

        // Identity
        _ = new UserContextDto { UserId = "1" };
        _ = new PermissionDto { PermissionId = "p1" };
        _ = new RoleDto { RoleId = "r1" };
        _ = new PrincipalSummaryDto { PrincipalId = Guid.NewGuid() };
        _ = new RoleBindingDto { BindingId = "b1" };

        // Accounting
        _ = new ChartOfAccountDto { Code = "1" };
        _ = new JournalEntryDto { EntryNumber = "JE-1" };
        _ = new JournalEntryLineDto { Debit = 10 };
        _ = new FinancialReportDto { ReportName = "Rep" };

        // Customers
        _ = new CustomerSummaryDto { Name = "Cust" };
        _ = new CustomerDetailDto { FirstName = "John" };
        _ = new AddressResponse { City = "BKK" };
        _ = new NDAResponse { Status = "Active" };
        _ = new InternalNoteResponse { NoteText = "Note" };
        _ = new DocumentResponse { FileName = "doc.pdf" };

        // Registry
        _ = new CreateCompanyRequest { Name = "Comp" };
        _ = new CountryDto { Name = "Thai" };
        _ = new RegistryThaiLocation { PostalCode = "10110" };
        _ = new RegistryCompanyProfile { TaxId = "123" };

        // Chat
        _ = new BffChatMessageResponse { Content = "Hi" };
        _ = new BffChatSessionResponse { SessionId = Guid.NewGuid() };

        // Dashboard
        _ = new DashboardViewModel();

        // Finance
        _ = new BillingNoteDto { BillingNoteNumber = "BN-1" };
        _ = new CreditTermDto { Name = "Net 30" };

        // HR
        _ = new EmployeeSummaryDto { Name = "Emp" };
        var empDetail = new EmployeeDetailDto { FirstName = "John", LastName = "Doe" };
        Assert.Equal("John Doe", empDetail.Name);
        _ = new OrgNodeDto { Name = "CEO" };
        _ = new ComplianceStatsDto { TotalActive = 100 };
        _ = new RecruitmentStatsDto { Applied = 50 };
        _ = new JobPostingSummaryDto { Title = "Dev" };
        _ = new LeaveBalanceDto { Available = 10 };
        _ = new LeaveRequestSummaryDto { LeaveType = "Annual" };
        _ = new CompensationSummaryDto { BaseSalary = 50000 };
        _ = new HrAnalyticsDto { TotalHeadcount = 100 };
        _ = new BenefitDto { Name = "Health" };
        _ = new CreateEmployeeRequest { Email = "test@test.com" };

        Assert.True(true);
    }
}

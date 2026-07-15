using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
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
        _ = new RoleBindingDto { BindingId = Guid.NewGuid() };

        // Accounting
        _ = new ChartOfAccountDto { Code = "1" };
        _ = new JournalEntryDto { EntryNumber = "JE-1" };
        _ = new JournalEntryLineDto { Debit = 10 };
        _ = new FinancialReportDto { ReportName = "Rep" };
        _ = new FinancialReportPdfData { ReportTitle = "Income Statement" };
        _ = new FinancialReportPdfSection { SectionTitle = "Revenue" };
        _ = new FinancialReportPdfLineItem { Description = "4000 - Revenue" };

        // Customers
        _ = new CustomerSummaryDto { Name = "Cust" };
        _ = new CustomerDetailDto { FirstName = "John" };
        _ = new AddressResponse { City = "BKK" };
        _ = new GoogleAddressConfigResponse { ApiKey = "browser-key" };
        _ = new GoogleAddressSelection { Source = "GooglePlace", PlaceId = "place-1" };
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

    [Fact]
    public void AddressDtos_GoogleMetadata_UseCamelCaseWireNames()
    {
        var request = new CreateAddressRequest
        {
            Type = "Shipping",
            AddressLine1 = "88 Rama IX Road",
            City = "Huai Khwang",
            StateProvince = "Bangkok",
            PostalCode = "10310",
            CountryId = Guid.NewGuid(),
            PlaceLabel = "Work",
            DriverNote = "Call before delivery",
            AddressSource = "GooglePlace",
            GooglePlaceId = "ChIJ-test",
            FormattedAddress = "MALIEV Co., Ltd., Bangkok",
            Latitude = 13.7563m,
            Longitude = 100.5018m
        };

        var json = JsonSerializer.Serialize(request);

        Assert.Contains("\"placeLabel\":\"Work\"", json, StringComparison.Ordinal);
        Assert.Contains("\"driverNote\":\"Call before delivery\"", json, StringComparison.Ordinal);
        Assert.Contains("\"addressSource\":\"GooglePlace\"", json, StringComparison.Ordinal);
        Assert.Contains("\"googlePlaceId\":\"ChIJ-test\"", json, StringComparison.Ordinal);
        Assert.Contains("\"formattedAddress\":\"MALIEV Co., Ltd., Bangkok\"", json, StringComparison.Ordinal);
        Assert.Contains("\"latitude\":13.7563", json, StringComparison.Ordinal);
        Assert.Contains("\"longitude\":100.5018", json, StringComparison.Ordinal);
    }

    [Fact]
    public void QuotationPdfItem_LegacyAliases_PopulateRichTemplateFields()
    {
        var item = new QuotationPdfItem
        {
            Description = "Bracket",
            TotalPrice = 250m
        };

        Assert.Equal("Bracket", item.MaterialName);
        Assert.Equal(250m, item.LineTotal);
    }

    [Fact]
    public void QuotationPdfItem_LegacyJsonFields_PopulateRichTemplateFields()
    {
        var item = JsonSerializer.Deserialize<QuotationPdfItem>("""{"description":"Bracket","totalPrice":250}""");

        Assert.NotNull(item);
        Assert.Equal("Bracket", item.MaterialName);
        Assert.Equal(250m, item.LineTotal);
    }
}

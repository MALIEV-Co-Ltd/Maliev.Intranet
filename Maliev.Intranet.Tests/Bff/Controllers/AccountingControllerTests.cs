using System.Reflection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public sealed class AccountingControllerTests
{
    [Theory]
    [InlineData(nameof(AccountingController.GetAccountsTree), MalievPermissions.Accounting.Read)]
    [InlineData(nameof(AccountingController.GetJournalEntries), MalievPermissions.Accounting.Journal.Read)]
    [InlineData(nameof(AccountingController.CreateJournalEntry), MalievPermissions.Accounting.Journal.Create)]
    [InlineData(nameof(AccountingController.PostJournalEntry), MalievPermissions.Accounting.Journal.Post)]
    [InlineData(nameof(AccountingController.OpenPeriod), MalievPermissions.Accounting.PeriodsOpen)]
    [InlineData(nameof(AccountingController.ClosePeriod), MalievPermissions.Accounting.PeriodsClose)]
    [InlineData(nameof(AccountingController.ReopenPeriod), MalievPermissions.Accounting.PeriodsReopen)]
    [InlineData(nameof(AccountingController.RunReconciliation), MalievPermissions.Accounting.ReconciliationRun)]
    [InlineData(nameof(AccountingController.UploadDocument), MalievPermissions.Accounting.Journal.Create)]
    [InlineData(nameof(AccountingController.DownloadReportPdf), MalievPermissions.Accounting.Reports.Export)]
    public void AccountingEndpoints_RequireActualAccountingServicePermissions(string actionName, string expectedPermission)
    {
        var method = typeof(AccountingController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName);

        var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>());

        Assert.Contains(expectedPermission, attribute.Policy, StringComparison.Ordinal);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }

    [Fact]
    public async Task DownloadReportPdf_UsesPdfServiceReportContractAndReturnsPdfFile()
    {
        var start = new DateTime(2026, 5, 1);
        var end = new DateTime(2026, 5, 31);
        var pdfBytes = "%PDF-1.4\nreport\n%%EOF"u8.ToArray();
        JsonElement? pdfRequestPayload = null;
        HttpRequestMessage? downloadRequest = null;
        var accountingClient = new Mock<IAccountingServiceClient>();
        accountingClient
            .Setup(client => client.GetReportAsync("income-statement", start, end, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinancialReportDto
            {
                ReportName = "Income Statement",
                GeneratedAt = new DateTime(2026, 5, 17, 2, 30, 0, DateTimeKind.Utc),
                Sections =
                [
                    new ReportSectionDto
                    {
                        Title = "Revenue",
                        Total = 12500m,
                        Rows =
                        [
                            new ReportRowDto { Label = "4000 - Revenue", Amount = 12500m }
                        ]
                    },
                    new ReportSectionDto
                    {
                        Title = "Expenses",
                        Total = 3500m,
                        Rows =
                        [
                            new ReportRowDto { Label = "9200 - Bank Fees", Amount = 3500m }
                        ]
                    }
                ]
            });
        var pdfHandler = new MockHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/pdf/v1/generations/generate", request.RequestUri!.AbsolutePath);
            pdfRequestPayload = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { storageUrl = "https://storage.example.com/accounting-report.pdf" })
            };
        });
        var downloadHandler = new MockHttpMessageHandler((request, _) =>
        {
            downloadRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(pdfBytes)
            });
        });
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(downloadHandler));
        var controller = new AccountingController(
            accountingClient.Object,
            pdfClient: new PdfServiceClient(new HttpClient(pdfHandler) { BaseAddress = new Uri("http://test") }),
            httpClientFactory: httpClientFactory.Object);

        var result = await controller.DownloadReportPdf("income-statement", start, end, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("income-statement_2026-05-01_2026-05-31.pdf", file.FileDownloadName);
        Assert.Equal(pdfBytes, file.FileContents);
        Assert.NotNull(downloadRequest);
        Assert.Equal(new Uri("https://storage.example.com/accounting-report.pdf"), downloadRequest.RequestUri);
        Assert.NotNull(pdfRequestPayload);
        var requestJson = pdfRequestPayload.Value;
        Assert.Equal("Report", requestJson.GetProperty("documentType").GetString());
        Assert.Equal("Report", requestJson.GetProperty("templateCode").GetString());
        Assert.Equal("income-statement-20260501-20260531", requestJson.GetProperty("referenceId").GetString());
        var data = requestJson.GetProperty("data");
        Assert.Equal("Income Statement", data.GetProperty("ReportTitle").GetString());
        Assert.Equal("RPT-INCOME-STATEMENT-20260501-20260531", data.GetProperty("ReportNumber").GetString());
        Assert.Equal("MALIEV Co., Ltd.", data.GetProperty("CompanyName").GetString());
        Assert.Equal("THB", data.GetProperty("Currency").GetString());
        Assert.Equal(12500d, data.GetProperty("TotalRevenue").GetDouble());
        Assert.Equal(3500d, data.GetProperty("TotalExpenses").GetDouble());
        Assert.Equal(9000d, data.GetProperty("NetProfit").GetDouble());
        var firstSection = data.GetProperty("Sections")[0];
        Assert.Equal("Revenue", firstSection.GetProperty("SectionTitle").GetString());
        Assert.Equal("4000 - Revenue", firstSection.GetProperty("LineItems")[0].GetProperty("Description").GetString());
    }
}

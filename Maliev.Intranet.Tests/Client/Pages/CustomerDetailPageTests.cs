using Bunit;
using Maliev.Intranet.Client.Pages.Customers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.Intranet.Tests.Client.Pages;

public sealed class CustomerDetailPageTests : BunitContext, IAsyncLifetime
{
    private readonly Guid _customerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _accountManagerId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private readonly List<string> _requestedPaths = [];

    public CustomerDetailPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler(HandleRequestAsync);
        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://test/") });

        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void CustomerDetail_RendersReferenceCrmRecordLayout()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Sarah Chen", cut.Markup));

        Assert.Contains("customer-record-shell", cut.Markup);
        Assert.Contains("customer-record-tabs", cut.Markup);
        Assert.Contains("Overview", cut.Markup);
        Assert.Contains("Addresses (2)", cut.Markup);
        Assert.Contains("Projects (1)", cut.Markup);
        Assert.Contains("Orders", cut.Markup);
        Assert.Contains("Notes (1)", cut.Markup);
        Assert.Contains("Activity", cut.Markup);
        Assert.Contains("Contact information", cut.Markup);
        Assert.Contains("Account &amp; billing", cut.Markup);
        Assert.Contains("Account manager", cut.Markup);
        Assert.Contains("Recent projects", cut.Markup);
        Assert.Contains("Recent orders", cut.Markup);
        Assert.Contains("Snapshot", cut.Markup);
        Assert.Contains("Default addresses", cut.Markup);
        Assert.Contains("Recent activity", cut.Markup);
        Assert.Contains("Link", cut.Markup);
        Assert.DoesNotContain("Impersonate", cut.Markup);
        Assert.DoesNotContain("Password reset", cut.Markup);
        Assert.Contains("Discard", cut.Markup);
        Assert.Contains("Save", cut.Markup);
        Assert.DoesNotContain("mlv-stat-tile", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerDetail_LoadsEmployeesForAccountManagerSelector()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Mia Wong - Sales Manager", cut.Markup));

        Assert.Contains(_requestedPaths, path => path.Equals("/api/v1/employees?page=1&pageSize=100", StringComparison.Ordinal));
        var option = cut.Find($"option[value='{_accountManagerId}']");
        Assert.Equal("Mia Wong - Sales Manager", option.TextContent);
    }

    [Fact]
    public void CustomerDetail_LoadsProjectsSeparatelyFromOrders()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("PRJ-2026-014", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("Robot arm bracket batch", cut.Markup));

        Assert.Contains(_requestedPaths, path => path.Equals($"/api/v1/projects?customerId={_customerId}&page=1&pageSize=20", StringComparison.Ordinal));

        cut.Find("button[data-tab='projects']").Click();

        Assert.Contains("All projects (1)", cut.Markup);
        Assert.Contains("customer-projects-panel", cut.Markup);
        Assert.Contains("PRJ-2026-014", cut.Markup);
        Assert.Contains("Robot arm bracket batch", cut.Markup);
        Assert.Contains("4", cut.Markup);
        Assert.Contains("$9,800", cut.Markup);
    }

    [Fact]
    public void CustomerDetail_LoadsOrdersAndActivity_ForSidebarAndRecentOrders()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Q-2026-098", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("Order Q-2026-098 paid", cut.Markup));

        Assert.Contains(_requestedPaths, path => path.Contains($"/api/v1/orders?customerId={_customerId}", StringComparison.Ordinal));
        Assert.Contains(_requestedPaths, path => path.Contains($"/api/v1/customers/{_customerId}/history", StringComparison.Ordinal));
    }

    [Fact]
    public void CustomerDetail_TabsRenderConsistentDetailSections()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Sarah Chen", cut.Markup));

        cut.Find("button[data-tab='addresses']").Click();
        Assert.Contains("Address book", cut.Markup);
        Assert.Contains("customer-address-book-grid", cut.Markup);
        Assert.Contains("customer-address-card", cut.Markup);
        Assert.DoesNotContain("Contact information", cut.Markup, StringComparison.Ordinal);

        cut.Find("button[data-tab='projects']").Click();
        Assert.Contains("All projects (1)", cut.Markup);
        Assert.Contains("customer-projects-panel", cut.Markup);
        Assert.Contains("PRJ-2026-014", cut.Markup);

        cut.Find("button[data-tab='orders']").Click();
        Assert.Contains("All orders (1)", cut.Markup);
        Assert.Contains("customer-orders-panel", cut.Markup);
        Assert.Contains("Q-2026-098", cut.Markup);
        Assert.Contains("Order #", cut.Markup);
        Assert.DoesNotContain("Quote #", cut.Markup, StringComparison.Ordinal);

        cut.Find("button[data-tab='notes']").Click();
        Assert.Contains("Add internal note", cut.Markup);
        Assert.Contains("customer-notes-layout", cut.Markup);
        Assert.Contains("Added internal note", cut.Markup);

        cut.Find("button[data-tab='activity']").Click();
        Assert.Contains("Audit trail", cut.Markup);
        Assert.Contains("customer-audit-list", cut.Markup);
        Assert.Contains("Order Q-2026-098 paid", cut.Markup);
    }

    [Fact]
    public void CustomerDetail_ActionsUseRecordStyleModals()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Sarah Chen", cut.Markup));

        cut.Find(".customer-split-field .customer-inline-action").Click();
        Assert.Contains("Link company", cut.Markup);
        Assert.Contains("Company lookup", cut.Markup);
        Assert.Contains("Company name", cut.Markup);

        cut.Find("button[aria-label='Close']").Click();
        cut.Find("button[data-tab='addresses']").Click();
        cut.Find("button.customer-address-edit").Click();
        Assert.Contains("Edit address", cut.Markup);
        Assert.Contains("customer-address-form", cut.Markup);
        Assert.Contains("HQ - Billing", cut.Markup);
        Assert.Contains("Recipient name", cut.Markup);
        Assert.Contains("Address lookup", cut.Markup);
        Assert.Contains("Country", cut.Markup);
    }

    private Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request, CancellationToken _)
    {
        var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;
        _requestedPaths.Add(pathAndQuery);

        if (pathAndQuery.StartsWith($"/api/v1/customers/{_customerId}/history", StringComparison.Ordinal))
        {
            return Json(new PagedResponse<CustomerActivityResponse>
            {
                Data =
                [
                    new CustomerActivityResponse
                    {
                        Action = "OrderPaid",
                        Description = "Order Q-2026-098 paid",
                        ActorName = "System",
                        Timestamp = new DateTime(2026, 4, 18, 14, 22, 0, DateTimeKind.Utc)
                    }
                ],
                Meta = new PaginationMeta { CurrentPage = 1, PageSize = 8, TotalCount = 1, TotalItems = 1, TotalPages = 1 }
            });
        }

        if (pathAndQuery.StartsWith("/api/v1/projects", StringComparison.Ordinal))
        {
            return Json(new PagedResponse<ProjectSummaryDto>
            {
                Data =
                [
                    new ProjectSummaryDto
                    {
                        Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                        ProjectNumber = "PRJ-2026-014",
                        CustomerId = _customerId,
                        CustomerName = "Sarah Chen",
                        Title = "Robot arm bracket batch",
                        Status = "Configuring",
                        PartsCount = 4,
                        TotalPrice = 9800m,
                        CreatedAt = new DateTime(2026, 4, 25, 0, 0, 0, DateTimeKind.Utc)
                    }
                ],
                Meta = new PaginationMeta { CurrentPage = 1, PageSize = 20, TotalCount = 1, TotalItems = 1, TotalPages = 1 }
            });
        }

        if (pathAndQuery.StartsWith("/api/v1/orders", StringComparison.Ordinal))
        {
            return Json(new PagedResponse<OrderSummaryDto>
            {
                Data =
                [
                    new OrderSummaryDto
                    {
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        OrderNumber = "Q-2026-098",
                        CustomerName = "Sarah Chen",
                        TotalAmount = 12400m,
                        Status = "Paid",
                        CreatedAt = new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc)
                    }
                ],
                Meta = new PaginationMeta { CurrentPage = 1, PageSize = 6, TotalCount = 1, TotalItems = 1, TotalPages = 1 }
            });
        }

        if (pathAndQuery.Equals("/api/v1/customers/countries", StringComparison.Ordinal))
        {
            return Json(new List<CountryDto>
            {
                new()
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Code = "US",
                    Name = "United States"
                }
            });
        }

        if (pathAndQuery.Equals("/api/v1/employees?page=1&pageSize=100", StringComparison.Ordinal))
        {
            return Json(new PagedResponse<EmployeeSummaryDto>
            {
                Data =
                [
                    new EmployeeSummaryDto
                    {
                        Id = _accountManagerId,
                        Name = "Mia Wong",
                        Email = "mia.wong@maliev.com",
                        Title = "Sales Manager",
                        Status = "Active"
                    }
                ],
                Meta = new PaginationMeta { CurrentPage = 1, PageSize = 100, TotalCount = 1, TotalItems = 1, TotalPages = 1 }
            });
        }

        if (pathAndQuery.Equals($"/api/v1/customers/{_customerId}", StringComparison.Ordinal))
        {
            return Json(new CustomerDetailDto
            {
                Id = _customerId,
                Name = "Sarah Chen",
                FirstName = "Sarah",
                LastName = "Chen",
                Email = "sarah@axion.io",
                Mobile = "+1 415 555 0142",
                Status = "Active",
                Segment = "Enterprise",
                Tier = "Company",
                TotalSpent = 28400m,
                ActiveOrdersCount = 2,
                OpenQuotationsCount = 1,
                CreatedAt = new DateTime(2023, 3, 12, 0, 0, 0, DateTimeKind.Utc),
                CompanyId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                CompanyName = "Axion Robotics",
                CompanyVatNumber = "EIN 87-2341098",
                AccountManagerEmployeeId = _accountManagerId,
                CreatedByName = "Alex Kim",
                Addresses =
                [
                    new AddressResponse
                    {
                        Type = "Billing",
                        CountryId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                        Xmin = 101,
                        IsDefault = true,
                        RecipientName = "HQ - Billing",
                        AddressLine1 = "100 Tech Blvd",
                        AddressLine2 = "Suite 400",
                        City = "San Jose",
                        StateProvince = "CA",
                        PostalCode = "95128"
                    },
                    new AddressResponse
                    {
                        Type = "Shipping",
                        CountryId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                        Xmin = 102,
                        IsDefault = true,
                        RecipientName = "Manufacturing Dock",
                        AddressLine1 = "2200 Industrial Pkwy",
                        City = "Fremont",
                        StateProvince = "CA",
                        PostalCode = "94538"
                    }
                ],
                Notes =
                [
                    new InternalNoteResponse
                    {
                        NoteText = "Added internal note",
                        CreatedByName = "Alex Kim",
                        CreatedAt = new DateTime(2026, 4, 12, 9, 15, 0, DateTimeKind.Utc)
                    }
                ]
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new object())
        });
    }

    private static Task<HttpResponseMessage> Json<T>(T body) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(body)
        });
}

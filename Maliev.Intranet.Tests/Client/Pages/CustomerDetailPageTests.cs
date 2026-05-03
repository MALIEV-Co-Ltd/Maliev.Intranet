using Bunit;
using Maliev.Intranet.Client.Pages.Customers;
using Maliev.Intranet.Shared;
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
        Assert.Contains("Orders", cut.Markup);
        Assert.Contains("Notes (1)", cut.Markup);
        Assert.Contains("Activity", cut.Markup);
        Assert.Contains("Contact information", cut.Markup);
        Assert.Contains("Account &amp; billing", cut.Markup);
        Assert.Contains("Recent orders", cut.Markup);
        Assert.Contains("Snapshot", cut.Markup);
        Assert.Contains("Default addresses", cut.Markup);
        Assert.Contains("Recent activity", cut.Markup);
        Assert.Contains("Password reset", cut.Markup);
        Assert.Contains("Impersonate", cut.Markup);
        Assert.Contains("Discard", cut.Markup);
        Assert.Contains("Save", cut.Markup);
        Assert.DoesNotContain("mlv-stat-tile", cut.Markup, StringComparison.Ordinal);
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

        cut.Find("button[data-tab='orders']").Click();
        Assert.Contains("All orders (1)", cut.Markup);
        Assert.Contains("customer-orders-panel", cut.Markup);
        Assert.Contains("Q-2026-098", cut.Markup);

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

        cut.Find("button.customer-action-email").Click();
        Assert.Contains("Send email", cut.Markup);
        Assert.Contains("customer-modal-wide", cut.Markup);
        Assert.Contains("Invoice reminder", cut.Markup);
        Assert.Contains("Quote follow-up", cut.Markup);

        cut.Find("button[aria-label='Close']").Click();
        cut.Find("button.customer-action-password").Click();
        Assert.Contains("Send password reset?", cut.Markup);
        Assert.Contains("A password reset link will be emailed to sarah@axion.io.", cut.Markup);

        cut.Find("button[aria-label='Close']").Click();
        cut.Find("button[data-tab='addresses']").Click();
        cut.Find("button.customer-address-edit").Click();
        Assert.Contains("Edit address", cut.Markup);
        Assert.Contains("customer-address-form", cut.Markup);
        Assert.Contains("HQ - Billing", cut.Markup);
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
                CreatedByName = "Alex Kim",
                Addresses =
                [
                    new AddressResponse
                    {
                        Type = "Billing",
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

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
using System.Text.Json;

namespace Maliev.Intranet.Tests.Client.Pages;

public sealed class CustomerDetailPageTests : BunitContext, IAsyncLifetime
{
    private readonly Guid _customerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _accountManagerId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private readonly List<string> _requestedPaths = [];
    private readonly List<CustomerEmailRequest> _emailRequests = [];
    private readonly List<JsonDocument> _customerUpdatePayloads = [];

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
    public void CustomerDetail_SavePostsSelectedPaymentTerms()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Sarah Chen", cut.Markup));

        cut.Find(".customer-payment-term-trigger").Click();
        cut.FindAll(".customer-payment-term-option")
            .Single(option => option.TextContent.Contains("Net 45", StringComparison.Ordinal))
            .Click();
        cut.FindAll("button").Single(button => button.TextContent.Contains("Save", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Single(_customerUpdatePayloads));

        var root = _customerUpdatePayloads.Single().RootElement;
        Assert.Equal("Net 45", root.GetProperty("paymentTerms").GetString());
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
    public void CustomerDetail_ActivityTrail_DoesNotRenderRawCompanyIds()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Customer profile update: changed company assignment to Axion Robotics.", cut.Markup));
        cut.Find("button[data-tab='activity']").Click();

        Assert.Contains("Customer profile update: changed company assignment to Axion Robotics.", cut.Markup);
        Assert.DoesNotContain("companyid", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("db741b8f", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("efdd1db7", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("**", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerDetail_ActivityTrail_DoesNotRenderRawAccountManagerField()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Customer profile update: changed account manager to Mia Wong - Sales Manager.", cut.Markup));
        cut.Find("button[data-tab='activity']").Click();

        Assert.Contains("Customer profile update: changed account manager to Mia Wong - Sales Manager.", cut.Markup);
        Assert.DoesNotContain("accountmanageremployeeid", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(_accountManagerId.ToString(), cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("**", cut.Markup, StringComparison.Ordinal);
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

    [Fact]
    public void CustomerDetail_AddressAutocomplete_ThaiInputShowsThaiSuggestionAndPopulatesThaiFields()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Sarah Chen", cut.Markup));
        cut.Find("button[data-tab='addresses']").Click();
        cut.Find("button.customer-address-edit").Click();

        cut.FindAll(".customer-suggestion-field input").ToList()[1].Input("คลองข่อย");

        cut.WaitForAssertion(() => Assert.Contains("คลองข่อย, ปากเกร็ด, นนทบุรี 11120", cut.Markup));
        cut.Find(".customer-autocomplete-list button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("value=\"คลองข่อย\"", cut.Markup);
            Assert.Contains("value=\"ปากเกร็ด\"", cut.Markup);
            Assert.Contains("value=\"นนทบุรี\"", cut.Markup);
            Assert.Contains("value=\"11120\"", cut.Markup);
        });
    }

    [Fact]
    public void CustomerDetail_AddressAutocomplete_EnglishInputShowsEnglishSuggestionAndPopulatesEnglishFields()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Sarah Chen", cut.Markup));
        cut.Find("button[data-tab='addresses']").Click();
        cut.Find("button.customer-address-edit").Click();

        cut.FindAll(".customer-suggestion-field input").ToList()[1].Input("Khlong Khoi");

        cut.WaitForAssertion(() => Assert.Contains("Khlong Khoi, Pak Kret, Nonthaburi 11120", cut.Markup));
        cut.Find(".customer-autocomplete-list button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("value=\"Khlong Khoi\"", cut.Markup);
            Assert.Contains("value=\"Pak Kret\"", cut.Markup);
            Assert.Contains("value=\"Nonthaburi\"", cut.Markup);
            Assert.Contains("value=\"11120\"", cut.Markup);
        });
    }

    [Fact]
    public void CustomerDetail_InternalNoteComposer_EnablesImmediatelyAndShowsDatabaseLimit()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Sarah Chen", cut.Markup));
        cut.Find("button[data-tab='notes']").Click();

        var noteInput = cut.Find("textarea.customer-internal-note-input");
        var postButton = cut.Find("button.customer-post-note");

        Assert.True(postButton.HasAttribute("disabled"));
        Assert.Contains("0 / 5,000 characters", cut.Markup);
        Assert.Equal("5000", noteInput.GetAttribute("maxlength"));

        noteInput.Input("Call customer before releasing drawings.");

        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.Find("button.customer-post-note").HasAttribute("disabled"));
            Assert.Contains("40 / 5,000 characters", cut.Markup);
        });
    }

    [Fact]
    public void CustomerDetail_InternalNotesUseMultilineTextStyle()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Sarah Chen", cut.Markup));
        cut.Find("button[data-tab='notes']").Click();

        var note = cut.Find(".customer-note-text");

        Assert.Equal("p", note.TagName, ignoreCase: true);
        Assert.Contains("Added internal note", note.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomerDetail_SendEmail_OpensComposeDialogWithoutPosting()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Sarah Chen", cut.Markup));

        cut.Find("button.customer-email-open").Click();

        Assert.Contains("Email customer", cut.Markup);
        Assert.Contains("sarah@axion.io", cut.Markup);
        Assert.Contains("customer-email-subject", cut.Markup);
        Assert.Contains("customer-email-body", cut.Markup);
        Assert.Empty(_emailRequests);
        Assert.DoesNotContain(_requestedPaths, path => path.Equals($"/api/v1/customers/{_customerId}/email", StringComparison.Ordinal));
    }

    [Fact]
    public void CustomerDetail_SendEmailDialog_PostsTypedSubjectAndBody()
    {
        var cut = Render<CustomerDetail>(parameters => parameters.Add(page => page.Id, _customerId));

        cut.WaitForAssertion(() => Assert.Contains("Sarah Chen", cut.Markup));

        cut.Find("button.customer-email-open").Click();
        cut.Find("input.customer-email-subject").Input("Updated production schedule");
        cut.Find("textarea.customer-email-body").Input("Please review the attached production schedule before tomorrow.");
        cut.Find("button.customer-email-send").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(_emailRequests);
            Assert.Contains("Email queued for delivery.", cut.Markup);
        });

        var request = _emailRequests.Single();
        Assert.Equal("Updated production schedule", request.Subject);
        Assert.Equal("Please review the attached production schedule before tomorrow.", request.Body);
        Assert.DoesNotContain("Message from MALIEV", request.Subject, StringComparison.Ordinal);
        Assert.DoesNotContain("MALIEV is contacting you about your account.", request.Body, StringComparison.Ordinal);
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
                    },
                    new CustomerActivityResponse
                    {
                        Action = "Update",
                        Description = "Customer profile update: changed companyid from '**db741b8f-67cf-40ba-8db8-5b899ad80001**' to '**efdd1db7-7225-4c40-914f-83dc3af80002**'",
                        ActorName = "Natthapol Vanasrivilai",
                        Timestamp = new DateTime(2026, 5, 4, 5, 48, 0, DateTimeKind.Utc),
                        Details = "{\"CompanyId\":\"efdd1db7-7225-4c40-914f-83dc3af80002\"}"
                    },
                    new CustomerActivityResponse
                    {
                        Action = "Update",
                        Description = $"Customer profile update: set accountmanageremployeeid to '**{_accountManagerId}**'",
                        ActorName = "Natthapol Vanasrivilai",
                        Timestamp = new DateTime(2026, 5, 6, 3, 29, 0, DateTimeKind.Utc),
                        Details = $"{{\"AccountManagerEmployeeId\":\"{_accountManagerId}\"}}"
                    }
                ],
                Meta = new PaginationMeta { CurrentPage = 1, PageSize = 8, TotalCount = 3, TotalItems = 3, TotalPages = 1 }
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
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    Code = "TH",
                    Name = "Thailand"
                },
                new()
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Code = "US",
                    Name = "United States"
                }
            });
        }

        if (pathAndQuery.StartsWith("/api/v1/customers/locations/thai/multi", StringComparison.Ordinal))
        {
            return Json(new List<RegistryThaiLocation>
            {
                new()
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    PostalCode = "11120",
                    SubDistrictTh = "คลองข่อย",
                    DistrictTh = "ปากเกร็ด",
                    ProvinceTh = "นนทบุรี",
                    SubDistrictEn = "Khlong Khoi",
                    DistrictEn = "Pak Kret",
                    ProvinceEn = "Nonthaburi"
                }
            });
        }

        if (pathAndQuery.StartsWith("/api/v1/customers/locations/thai/geocode", StringComparison.Ordinal))
        {
            return Json(new AddressGeocodeResponse
            {
                Latitude = 13.912,
                Longitude = 100.503,
                DisplayName = "Khlong Khoi, Pak Kret, Nonthaburi, Thailand"
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

        if (pathAndQuery.Equals("/api/v1/customers/payment-terms", StringComparison.Ordinal))
        {
            return Json(new List<PaymentTermDto>
            {
                new()
                {
                    Code = "DUE_ON_RECEIPT",
                    Name = "Due on receipt",
                    Category = "Immediate",
                    Description = "Payment is due as soon as the invoice is received.",
                    TypicalUse = "Use for one-off jobs and customers without approved credit.",
                    DueDays = 0,
                    IsDefault = true,
                    SortOrder = 0
                },
                new()
                {
                    Code = "NET_30",
                    Name = "Net 30",
                    Category = "Net",
                    Description = "Full invoice amount is due 30 calendar days after the invoice date.",
                    TypicalUse = "Use as the standard B2B trade-credit term.",
                    DueDays = 30,
                    SortOrder = 40
                },
                new()
                {
                    Code = "NET_45",
                    Name = "Net 45",
                    Category = "Net",
                    Description = "Full invoice amount is due 45 calendar days after the invoice date.",
                    TypicalUse = "Use for larger commercial customers that require longer approval cycles.",
                    DueDays = 45,
                    SortOrder = 45
                }
            });
        }

        if (request.Method == HttpMethod.Get &&
            pathAndQuery.Equals($"/api/v1/customers/{_customerId}", StringComparison.Ordinal))
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
                PaymentTerms = "Net 30",
                TotalSpent = 28400m,
                ActiveOrdersCount = 2,
                OpenQuotationsCount = 1,
                CreatedAt = new DateTime(2023, 3, 12, 0, 0, 0, DateTimeKind.Utc),
                CompanyId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                CompanyName = "Axion Robotics",
                CompanyVatNumber = "EIN 87-2341098",
                AccountManagerEmployeeId = _accountManagerId,
                AccountManagerName = "Mia Wong",
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

        if (pathAndQuery.Equals($"/api/v1/customers/{_customerId}/email", StringComparison.Ordinal))
        {
            var payload = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "{}";
            var emailRequest = JsonSerializer.Deserialize<CustomerEmailRequest>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (emailRequest is not null)
            {
                _emailRequests.Add(emailRequest);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = JsonContent.Create(new { messageId = Guid.Parse("77777777-7777-7777-7777-777777777777") })
            });
        }

        if (request.Method == HttpMethod.Patch &&
            pathAndQuery.Equals($"/api/v1/customers/{_customerId}", StringComparison.Ordinal))
        {
            var payload = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "{}";
            _customerUpdatePayloads.Add(JsonDocument.Parse(payload));

            return Json(new CustomerResponse
            {
                Id = _customerId,
                FirstName = "Sarah",
                LastName = "Chen",
                Email = "sarah@axion.io",
                PaymentTerms = "Net 45",
                Xmin = 204
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

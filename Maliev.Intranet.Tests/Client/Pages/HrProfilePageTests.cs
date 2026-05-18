using System.Net;
using System.Net.Http.Json;
using Bunit;
using Maliev.Intranet.Client.Pages.Hr;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Pages;

public sealed class HrProfilePageTests : BunitContext, IAsyncLifetime
{
    private readonly List<string> _requestedPaths = [];
    private string? _savedJson;
    private string? _savedPreferenceJson;

    public HrProfilePageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler(HandleRequestAsync);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddSingleton(new LayoutService(JSInterop.JSRuntime, NullLogger<LayoutService>.Instance));
        Services.AddSingleton(new CurrencyService(client, NullLogger<CurrencyService>.Instance));

        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void Profile_RendersSelfServiceEditFields()
    {
        var cut = Render<Profile>();

        cut.WaitForAssertion(() => Assert.Contains("Edit profile", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Edit profile", StringComparison.Ordinal)).Click();

        Assert.Contains("Preferred name", cut.Markup);
        Assert.Contains("Personal email", cut.Markup);
        Assert.Contains("Mobile phone", cut.Markup);
        Assert.Contains("profile-edit-grid", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("profile-input", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("mlv-input", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(_requestedPaths, path => path.Equals("/api/v1/employees/me/profile", StringComparison.Ordinal));
    }

    [Fact]
    public void Profile_RendersProfileDefaultsFromSelfServiceAndBffFallbacks()
    {
        var cut = Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("test@test.com", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Active", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Job Title", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Platform Owner", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Full Time", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("FullTime", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("2026", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Profile_SaveProfile_PutsSelfServiceProfileRequest()
    {
        var cut = Render<Profile>();

        cut.WaitForAssertion(() => Assert.Contains("Edit profile", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Edit profile", StringComparison.Ordinal)).Click();

        cut.Find("input").Input("M");
        cut.Find("input[type='email']").Input("mia.updated@example.com");
        cut.Find("input[type='tel']").Input("+66811111111");

        cut.FindAll("button").Single(button => button.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedPaths, path => path.Equals("/api/v1/employees/me/profile", StringComparison.Ordinal));
            Assert.Contains("\"preferredName\":\"M\"", _savedJson, StringComparison.Ordinal);
            Assert.Contains("\"personalEmail\":\"mia.updated@example.com\"", _savedJson, StringComparison.Ordinal);
            Assert.Contains("\"mobilePhone\":\"\\u002B66811111111\"", _savedJson, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Profile_PreferencesQueryTab_RendersPreferenceEditor()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://test/hr/profile?tab=preferences");
        var cut = Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Workspace defaults", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Save preferences", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Default currency", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Employee-specific defaults", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Full email signature", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Short email signature", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Teams and documents", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Profile_PreferencesQueryTab_DefaultsSignaturesFromEmployeeProfile()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://test/hr/profile?tab=preferences");
        var cut = Render<Profile>();

        cut.WaitForAssertion(() =>
        {
            var fullSignature = cut.Find("textarea[name='emailSignature']").GetAttribute("value") ?? string.Empty;
            var shortSignature = cut.Find("textarea[name='shortEmailSignature']").GetAttribute("value") ?? string.Empty;

            Assert.Contains("Best regards", fullSignature, StringComparison.Ordinal);
            Assert.Contains("Mia Wong", fullSignature, StringComparison.Ordinal);
            Assert.Contains("Maliev Co., Ltd.", fullSignature, StringComparison.Ordinal);
            Assert.Contains("CNC Manufacturing and 3D Printing Services", fullSignature, StringComparison.Ordinal);
            Assert.Contains("+66 (0)81-000-0000", fullSignature, StringComparison.Ordinal);
            Assert.Contains("test@test.com", fullSignature, StringComparison.Ordinal);
            Assert.Equal("Best regards\nMia Wong", shortSignature.Replace("\r\n", "\n", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void Profile_SavePreferences_PutsScopedPreferenceRequest()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://test/hr/profile?tab=preferences");
        var cut = Render<Profile>();

        cut.WaitForAssertion(() => Assert.Contains("Save preferences", cut.Markup, StringComparison.Ordinal));
        cut.Find("select[name='defaultCurrency']").Change("EUR");
        cut.Find("select[name='themeMode']").Change("light");
        cut.Find("textarea[name='emailSignature']").Input("MALIEV sales");
        cut.Find("textarea[name='shortEmailSignature']").Input("MALIEV");

        cut.FindAll("button").Single(button => button.TextContent.Contains("Save preferences", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedPaths, path => path.Equals("/api/v1/preferences/intranet-profile", StringComparison.Ordinal));
            Assert.Contains("\"scope\":\"intranet-profile\"", _savedPreferenceJson, StringComparison.Ordinal);
            Assert.Contains("\"defaultCurrency\":\"EUR\"", _savedPreferenceJson, StringComparison.Ordinal);
            Assert.Contains("\"themeMode\":\"light\"", _savedPreferenceJson, StringComparison.Ordinal);
            Assert.Contains("\"emailSignature\":\"MALIEV sales\"", _savedPreferenceJson, StringComparison.Ordinal);
            Assert.Contains("\"shortEmailSignature\":\"MALIEV\"", _savedPreferenceJson, StringComparison.Ordinal);
            Assert.Equal(ThemeMode.Light, Services.GetRequiredService<LayoutService>().CurrentMode);
            Assert.Equal("EUR", Services.GetRequiredService<CurrencyService>().Code);
            Assert.Contains("Preferences saved.", cut.Markup, StringComparison.Ordinal);
        });
    }

    private async Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requestedPaths.Add(request.RequestUri!.PathAndQuery);

        if (request.Method == HttpMethod.Get && request.RequestUri.PathAndQuery == "/api/v1/employees/me")
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new EmployeeDetailDto
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    FirstName = "Mia",
                    LastName = "Wong",
                    Email = "",
                    Department = "Sales",
                    Title = "",
                    Role = "",
                    Status = "",
                    Phone = "+66810000000",
                    EmployeeType = "",
                    HireDate = new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc)
                })
            };
        }

        if (request.Method == HttpMethod.Get && request.RequestUri.PathAndQuery == "/api/v1/employees/me/profile")
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new EmployeeSelfProfileDto
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    EmployeeNumber = "EMP-001",
                    FirstName = "Mia",
                    LastName = "Wong",
                    FullName = "Mia Wong",
                    PreferredName = "Mia",
                    WorkEmail = "test@test.com",
                    PersonalEmail = "mia.personal@example.com",
                    MobilePhone = "+66810000000",
                    EmploymentType = "FullTime",
                    EmploymentStatus = "Active",
                    StartDate = new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc)
                })
            };
        }

        if (request.Method == HttpMethod.Get && request.RequestUri.PathAndQuery == "/api/v1/preferences/intranet-profile")
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UserPreferenceDto
                {
                    PrincipalId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Scope = "intranet-profile",
                    PreferenceData = new Dictionary<string, object>
                    {
                        ["themeMode"] = "dark",
                        ["defaultCurrency"] = "USD",
                        ["timeZone"] = "Asia/Bangkok"
                    },
                    UpdatedAt = new DateTime(2026, 5, 17, 6, 30, 0, DateTimeKind.Utc)
                })
            };
        }

        if (request.Method == HttpMethod.Get && request.RequestUri.PathAndQuery == "/api/v1/referenceData/currencies")
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<CurrencyDto>
                {
                    new() { Code = "THB", Name = "Thai Baht", Symbol = "฿", DecimalPlaces = 2, IsActive = true, IsPrimary = true },
                    new() { Code = "USD", Name = "US Dollar", Symbol = "$", DecimalPlaces = 2, IsActive = true },
                    new() { Code = "EUR", Name = "Euro", Symbol = "€", DecimalPlaces = 2, IsActive = true }
                })
            };
        }

        if (request.Method == HttpMethod.Get && request.RequestUri.PathAndQuery == "/api/v1/referenceData/currencies/rate?from=THB&to=EUR")
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ExchangeRateResponse("THB", "EUR", 0.025m))
            };
        }

        if (request.Method == HttpMethod.Put && request.RequestUri.PathAndQuery == "/api/v1/employees/me/profile")
        {
            _savedJson = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new EmployeeSelfProfileDto
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    EmployeeNumber = "EMP-001",
                    FirstName = "Mia",
                    LastName = "Wong",
                    FullName = "Mia Wong",
                    PreferredName = "M",
                    WorkEmail = "mia.wong@maliev.com",
                    PersonalEmail = "mia.updated@example.com",
                    MobilePhone = "+66811111111",
                    EmploymentType = "FullTime",
                    EmploymentStatus = "Active"
                })
            };
        }

        if (request.Method == HttpMethod.Put && request.RequestUri.PathAndQuery == "/api/v1/preferences/intranet-profile")
        {
            _savedPreferenceJson = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UserPreferenceDto
                {
                    PrincipalId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Scope = "intranet-profile",
                    PreferenceData = new Dictionary<string, object>
                    {
                        ["themeMode"] = "light",
                        ["landingPage"] = "/dashboard",
                        ["defaultCurrency"] = "EUR",
                        ["language"] = "en-TH",
                        ["timeZone"] = "Asia/Bangkok",
                        ["dateFormat"] = "dd MMM yyyy",
                        ["compactWorkspace"] = false,
                        ["operationalDigest"] = true,
                        ["emailSignature"] = "MALIEV sales",
                        ["shortEmailSignature"] = "MALIEV"
                    },
                    UpdatedAt = new DateTime(2026, 5, 17, 6, 45, 0, DateTimeKind.Utc)
                })
            };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}

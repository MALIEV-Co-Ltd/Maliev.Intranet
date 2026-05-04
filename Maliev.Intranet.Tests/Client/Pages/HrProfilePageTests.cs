using System.Net;
using System.Net.Http.Json;
using Bunit;
using Maliev.Intranet.Client.Pages.Hr;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Pages;

public sealed class HrProfilePageTests : BunitContext, IAsyncLifetime
{
    private readonly List<string> _requestedPaths = [];
    private string? _savedJson;

    public HrProfilePageTests()
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

        cut.Find("input").Change("M");
        cut.Find("input[type='email']").Change("mia.updated@example.com");
        cut.Find("input[type='tel']").Change("+66811111111");

        cut.FindAll("button").Single(button => button.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_requestedPaths, path => path.Equals("/api/v1/employees/me/profile", StringComparison.Ordinal));
            Assert.Contains("\"preferredName\":\"M\"", _savedJson, StringComparison.Ordinal);
            Assert.Contains("\"personalEmail\":\"mia.updated@example.com\"", _savedJson, StringComparison.Ordinal);
            Assert.Contains("\"mobilePhone\":\"\\u002B66811111111\"", _savedJson, StringComparison.Ordinal);
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
                    Scope = "intranet-profile"
                })
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

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}

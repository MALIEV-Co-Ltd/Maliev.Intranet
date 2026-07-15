using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class QuickControllerTests
{
    private static HttpClient CreateClient(object response)
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) }));
        return new HttpClient(handler) { BaseAddress = new Uri("http://test") };
    }

    private static HttpClient CreateRawJsonClient(string json)
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        }));
        return new HttpClient(handler) { BaseAddress = new Uri("http://test") };
    }

    private static PdfServiceClient CreatePdfClient()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            var response = new { storageUrl = "http://test.pdf" };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) });
        });
        return new PdfServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    [Fact]
    public async Task Orders_Get_ReturnsOk()
    {
        var controller = new OrdersController(new OrderServiceClient(CreateClient(new PagedResponse<OrderSummaryDto>())));
        var result = await controller.Get(null, 1, null, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Quotations_Get_ReturnsOk()
    {
        var controller = new QuotationsController(new QuotationServiceClient(CreateClient(new PagedResponse<QuotationSummaryDto>())), CreatePdfClient());
        var result = await controller.Get(null, 1, 20);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Quotations_GetById_RefreshesVersionPdfUrlFromStoragePath()
    {
        var quotationId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var quotation = new QuotationDetailDto
        {
            Id = quotationId,
            QuotationNumber = "Q-100",
            CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CustomerName = "Axion Robotics",
            CurrentVersionNumber = 1,
            Status = "Generated",
            ValidityPeriodStart = DateTime.UtcNow,
            ValidityPeriodEnd = DateTime.UtcNow.AddDays(30),
            Versions =
            [
                new QuotationVersionDto
                {
                    VersionNumber = 1,
                    PdfArtifactUrl = "https://storage.example/stale.pdf?X-Goog-Signature=expired",
                    PdfArtifactStoragePath = "pdfs/quotation/Q-100/quote.pdf",
                    PdfGeneratedAt = DateTime.UtcNow.AddHours(-2)
                }
            ]
        };
        JsonDocument? signedUrlRequest = null;
        var uploadHandler = new MockHttpMessageHandler(async (req, ct) =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.EndsWith("/upload/v1/files/by-path/signed-url", req.RequestUri!.AbsolutePath);
            signedUrlRequest = JsonDocument.Parse(await req.Content!.ReadAsStringAsync(ct));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl = "https://storage.example/fresh.pdf?X-Goog-Signature=fresh" })
            };
        });

        var controller = new QuotationsController(
            new QuotationServiceClient(CreateClient(quotation)),
            CreatePdfClient(),
            new UploadServiceClient(new HttpClient(uploadHandler) { BaseAddress = new Uri("http://test") }));

        var result = await controller.GetById(quotationId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<QuotationDetailDto>(ok.Value);
        Assert.Equal("https://storage.example/fresh.pdf?X-Goog-Signature=fresh", Assert.Single(body.Versions).PdfArtifactUrl);
        Assert.NotNull(signedUrlRequest);
        Assert.Equal("pdfs/quotation/Q-100/quote.pdf", signedUrlRequest.RootElement.GetProperty("storagePath").GetString());
        Assert.Equal(10080, signedUrlRequest.RootElement.GetProperty("expirationMinutes").GetInt32());
    }

    [Fact]
    public async Task Quotations_GetById_WhenStoragePathIsMissing_RepairsCurrentVersionFromPdfServiceLatestArtifact()
    {
        var quotationId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var quotation = new QuotationDetailDto
        {
            Id = quotationId,
            QuotationNumber = "Q-LEGACY",
            CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CustomerName = "Axion Robotics",
            CurrentVersionNumber = 1,
            Status = "Generated",
            ValidityPeriodStart = DateTime.UtcNow,
            ValidityPeriodEnd = DateTime.UtcNow.AddDays(30),
            Versions =
            [
                new QuotationVersionDto
                {
                    VersionNumber = 1,
                    PdfArtifactUrl = "https://storage.example/stale.pdf?X-Goog-Signature=expired",
                    PdfGeneratedAt = DateTime.UtcNow.AddHours(-2)
                }
            ]
        };

        JsonDocument? signedUrlRequest = null;
        JsonDocument? attachRequest = null;
        var quotationHandler = new MockHttpMessageHandler(async (req, ct) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri!.AbsolutePath.EndsWith($"/quotation/v1/quotations/{quotationId}", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(quotation)
                };
            }

            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith($"/quotation/v1/quotations/{quotationId}/versions/1/pdf-artifact", StringComparison.Ordinal))
            {
                attachRequest = JsonDocument.Parse(await req.Content!.ReadAsStringAsync(ct));
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var pdfHandler = new MockHttpMessageHandler((req, ct) =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.EndsWith("/pdf/v1/generations/latest", req.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    storageUrl = "https://storage.example/legacy.pdf?X-Goog-Signature=expired",
                    storagePath = "pdfs/quotation/Q-LEGACY/legacy.pdf"
                })
            });
        });
        var uploadHandler = new MockHttpMessageHandler(async (req, ct) =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.EndsWith("/upload/v1/files/by-path/signed-url", req.RequestUri!.AbsolutePath);
            signedUrlRequest = JsonDocument.Parse(await req.Content!.ReadAsStringAsync(ct));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { signedUrl = "https://storage.example/fresh.pdf?X-Goog-Signature=fresh" })
            };
        });

        var controller = new QuotationsController(
            new QuotationServiceClient(new HttpClient(quotationHandler) { BaseAddress = new Uri("http://test") }),
            new PdfServiceClient(new HttpClient(pdfHandler) { BaseAddress = new Uri("http://test") }),
            new UploadServiceClient(new HttpClient(uploadHandler) { BaseAddress = new Uri("http://test") }));

        var result = await controller.GetById(quotationId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<QuotationDetailDto>(ok.Value);
        var version = Assert.Single(body.Versions);
        Assert.Equal("https://storage.example/fresh.pdf?X-Goog-Signature=fresh", version.PdfArtifactUrl);
        Assert.Equal("pdfs/quotation/Q-LEGACY/legacy.pdf", version.PdfArtifactStoragePath);
        Assert.NotNull(signedUrlRequest);
        Assert.Equal("pdfs/quotation/Q-LEGACY/legacy.pdf", signedUrlRequest.RootElement.GetProperty("storagePath").GetString());
        Assert.NotNull(attachRequest);
        Assert.Equal("https://storage.example/fresh.pdf?X-Goog-Signature=fresh", attachRequest.RootElement.GetProperty("pdfArtifactUrl").GetString());
        Assert.Equal("pdfs/quotation/Q-LEGACY/legacy.pdf", attachRequest.RootElement.GetProperty("pdfArtifactStoragePath").GetString());
    }

    [Fact]
    public async Task Quotations_GenerateDraftPdf_StampsQuotedByFromAuthenticatedEmployee()
    {
        JsonDocument? capturedRequest = null;
        var handler = new MockHttpMessageHandler(async (req, ct) =>
        {
            capturedRequest = JsonDocument.Parse(await req.Content!.ReadAsStringAsync(ct));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { storageUrl = "http://test.pdf" })
            };
        });
        var pdfClient = new PdfServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new QuotationsController(new QuotationServiceClient(CreateClient(new PagedResponse<QuotationSummaryDto>())), pdfClient)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Name, "Alex Kim"),
                        new Claim("email", "alex.kim@maliev.com"),
                        new Claim("phone_number", "+66810000000"),
                    ], "Test"))
                }
            }
        };

        var result = await controller.GenerateDraftPdf(new QuotationPdfData { QuotationNumber = "Q-DRAFT" }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(capturedRequest);
        var data = capturedRequest.RootElement.GetProperty("data");
        Assert.Equal("Alex Kim", data.GetProperty("QuotedByName").GetString());
        Assert.Equal("alex.kim@maliev.com", data.GetProperty("QuotedByEmail").GetString());
        Assert.Equal("+66810000000", data.GetProperty("QuotedByPhone").GetString());
        Assert.True(data.TryGetProperty("QuotedAt", out var quotedAt));
        Assert.False(string.IsNullOrWhiteSpace(quotedAt.GetString()));
    }

    [Fact]
    public async Task Quotations_GeneratePdf_UsesCurrentQuotationVersionForPdfItems()
    {
        JsonDocument? capturedRequest = null;
        var pdfHandler = new MockHttpMessageHandler(async (req, ct) =>
        {
            capturedRequest = JsonDocument.Parse(await req.Content!.ReadAsStringAsync(ct));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { storageUrl = "http://test.pdf" })
            };
        });

        var quotationId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var quotation = new QuotationDetailDto
        {
            Id = quotationId,
            QuotationNumber = "Q-100",
            CustomerName = "Axion Robotics",
            CurrencyCode = "THB",
            CurrentVersionNumber = 2,
            CreatedAt = new DateTime(2026, 4, 18, 8, 0, 0, DateTimeKind.Utc),
            ValidityPeriodStart = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc),
            ValidityPeriodEnd = new DateTime(2026, 5, 18, 0, 0, 0, DateTimeKind.Utc),
            Versions =
            [
                new QuotationVersionDto
                {
                    VersionNumber = 1,
                    LineItems = [new QuotationItemDto { Description = "Old line", Quantity = 1, UnitPrice = 10m }]
                },
                new QuotationVersionDto
                {
                    VersionNumber = 2,
                    DiscountStructure = new SalesDiscountStructureDto
                    {
                        DiscountType = SalesDiscountType.FixedAmount,
                        DiscountValue = 15m,
                        Conditions = "Automatic bulk order discount",
                    },
                    LineItems = [new QuotationItemDto { Description = "Current line", Quantity = 2, UnitPrice = 25m }]
                }
            ]
        };

        var controller = new QuotationsController(
            new QuotationServiceClient(CreateClient(quotation)),
            new PdfServiceClient(new HttpClient(pdfHandler) { BaseAddress = new Uri("http://test") }));

        var result = await controller.GeneratePdf(quotationId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(capturedRequest);
        var data = capturedRequest.RootElement.GetProperty("data");
        Assert.Equal(2, data.GetProperty("VersionNumber").GetInt32());
        var item = data.GetProperty("Items")[0];
        Assert.Equal("Current line", item.GetProperty("MaterialName").GetString());
        Assert.Equal(50m, item.GetProperty("LineTotal").GetDecimal());
        Assert.Equal(50m, data.GetProperty("SubtotalBeforeDiscount").GetDecimal());
        Assert.Equal(15m, data.GetProperty("TotalDiscount").GetDecimal());
        Assert.Equal(35m, data.GetProperty("Subtotal").GetDecimal());
        Assert.Equal("FixedAmount", data.GetProperty("Discounts")[0].GetProperty("DiscountType").GetString());
    }

    [Fact]
    public async Task QuotationServiceClient_GetQuotationById_AcceptsNumericStatusFromQuotationService()
    {
        var quotationId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var client = new QuotationServiceClient(CreateRawJsonClient($$"""
        {
          "id": "{{quotationId}}",
          "quotationNumber": "Q-100",
          "customerId": "11111111-1111-1111-1111-111111111111",
          "customerName": "Axion Robotics",
          "currentVersionNumber": 1,
          "status": 5,
          "validityPeriodStart": "2026-04-18T00:00:00Z",
          "validityPeriodEnd": "2026-05-18T00:00:00Z",
          "subTotal": 100,
          "tax": 7,
          "total": 107,
          "currencyCode": "THB",
          "versions": [],
          "internalNotes": [],
          "attachments": [],
          "createdAt": "2026-04-18T08:00:00Z",
          "updatedAt": "2026-04-18T08:00:00Z"
        }
        """));

        var quotation = await client.GetQuotationByIdAsync(quotationId);

        Assert.NotNull(quotation);
        Assert.Equal("Accepted", quotation.Status);
    }

    [Fact]
    public async Task Dashboard_Get_ReturnsOk()
    {
        var client = CreateClient(new { count = 10 });
        var controller = new DashboardController(
            new OrderServiceClient(client),
            new QuotationServiceClient(client),
            new PaymentServiceClient(client),
            new EmployeeServiceClient(client),
            new InvoiceServiceClient(client),
            new LeaveServiceClient(client),
            new ProjectServiceClient(client),
            new JobServiceClient(client));
        var result = await controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Diagnostics_GetMe_ReturnsOk()
    {
        var controller = new DiagnosticsController(new IAMServiceClient(CreateClient(new UserContextDto())));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var result = await controller.GetMe();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void ClientConfig_GetClientConfig_ReturnsDefaultProcessingTimeout()
    {
        var configuration = new ConfigurationBuilder().Build();
        var controller = new ClientConfigController(configuration);

        var result = controller.GetClientConfig();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ClientConfigDto>(ok.Value);
        Assert.Equal(3, dto.ProcessingTimeoutMinutes);
    }

    [Fact]
    public void ClientConfig_GetClientConfig_ReturnsConfiguredProcessingTimeout()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MalievClient:ProcessingTimeoutMinutes"] = "7"
            })
            .Build();
        var controller = new ClientConfigController(configuration);

        var result = controller.GetClientConfig();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ClientConfigDto>(ok.Value);
        Assert.Equal(7, dto.ProcessingTimeoutMinutes);
    }

    [Fact]
    public async Task Diagnostics_GetMe_ReturnsEffectiveIdentityAndIamResolution()
    {
        var principalId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var cacheUntil = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);
        var iamClient = new IAMServiceClient(CreateClient(new
        {
            principalId,
            permissions = new[] { MalievPermissions.System.DiagnosticsRead },
            roles = new[] { "roles.system.diagnostics" },
            resourcePath = "system/diagnostics",
            cacheUntil,
            fromCache = true
        }));
        var controller = new DiagnosticsController(iamClient)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("user_id", principalId.ToString()),
                        new Claim("email", "operator@maliev.com"),
                        new Claim("permissions", MalievPermissions.System.DiagnosticsRead)
                    ], "TestAuth"))
                }
            }
        };

        var result = await controller.GetMe();

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.True(root.GetProperty("IsAuthenticated").GetBoolean());
        Assert.Equal("TestAuth", root.GetProperty("AuthenticationType").GetString());
        Assert.Equal(principalId.ToString(), root.GetProperty("PrincipalId").GetString());
        Assert.Contains(root.GetProperty("Claims").EnumerateArray(), claim =>
            claim.GetProperty("Type").GetString() == "email"
            && claim.GetProperty("Value").GetString() == "operator@maliev.com");
        var iamData = root.GetProperty("IamResolvedData");
        Assert.Equal(principalId, iamData.GetProperty("PrincipalId").GetGuid());
        Assert.Equal(
            MalievPermissions.System.DiagnosticsRead,
            Assert.Single(iamData.GetProperty("Permissions").EnumerateArray()).GetString());
        Assert.Equal(
            "roles.system.diagnostics",
            Assert.Single(iamData.GetProperty("Roles").EnumerateArray()).GetString());
        Assert.Equal("system/diagnostics", iamData.GetProperty("ResourcePath").GetString());
        Assert.Equal(cacheUntil, iamData.GetProperty("CacheUntil").GetDateTime());
        Assert.True(iamData.GetProperty("FromCache").GetBoolean());
    }

    [Fact]
    public void DiagnosticsController_RequiresDiagnosticsReadPermission()
    {
        var attribute = Assert.Single(typeof(DiagnosticsController).GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true));
        var permission = Assert.IsType<RequirePermissionAttribute>(attribute);

        Assert.Equal(MalievPermissions.System.DiagnosticsRead, permission.Permission);
        Assert.Equal("Bearer,Cookies", permission.AuthenticationSchemes);
    }

    [Fact]
    public async Task Iam_GetPermissions_ReturnsOk()
    {
        var authMock = new Mock<IAuthorizationService>();
        authMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>())).ReturnsAsync(AuthorizationResult.Success());
        var controller = new IamController(new IAMServiceClient(CreateClient(new List<PermissionDto>())), authMock.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "u")], "Test")) } };
        var result = await controller.GetPermissions();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Iam_GetRolesPaged_FiltersAndPaginatesRoles()
    {
        var authMock = new Mock<IAuthorizationService>();
        authMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());
        var iamClient = new Mock<IAMServiceClient>(new HttpClient { BaseAddress = new Uri("http://iam") });
        iamClient.Setup(client => client.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RoleDto { RoleId = "roles.customer.viewer", ServiceName = "customer", Name = "Customer Viewer", Description = "Read-only access", PermissionIds = ["customer.customers.read"] },
                new RoleDto { RoleId = "roles.iam.admin", ServiceName = "iam", Name = "IAM Admin", Description = "Manage IAM", PermissionIds = ["iam.roles.create"] }
            ]);
        var controller = new IamController(iamClient.Object, authMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "u")], "Test")) } }
        };

        var result = await controller.GetRolesPaged(service: "customer", page: 1, pageSize: 10);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PagedResponse<RoleDto>>(ok.Value);
        var role = Assert.Single(response.Data);
        Assert.Equal("Customer Viewer", role.Name);
        Assert.Equal(1, response.Meta.TotalItems);
    }

    [Fact]
    public async Task Iam_GetPermissionsPaged_SearchesAndPaginatesPermissions()
    {
        var authMock = new Mock<IAuthorizationService>();
        authMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());
        var iamClient = new Mock<IAMServiceClient>(new HttpClient { BaseAddress = new Uri("http://iam") });
        iamClient.Setup(client => client.GetPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PermissionDto { PermissionId = "customer.customers.read", Name = "Read customers", Category = "Customer", Description = "Read customer records" },
                new PermissionDto { PermissionId = "iam.roles.create", Name = "Create roles", Category = "IAM", Description = "Create IAM roles" }
            ]);
        var controller = new IamController(iamClient.Object, authMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "u")], "Test")) } }
        };

        var result = await controller.GetPermissionsPaged(category: "IAM", page: 1, pageSize: 10);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PagedResponse<PermissionDto>>(ok.Value);
        var permission = Assert.Single(response.Data);
        Assert.Equal("iam.roles.create", permission.PermissionId);
        Assert.Equal(1, response.Meta.TotalItems);
    }
}

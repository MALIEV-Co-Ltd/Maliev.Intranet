using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public sealed class SupplierServiceClientTests
{
    [Fact]
    public async Task CreateSupplierAsync_MapsIntranetSupplierRequestToSupplierServiceContract()
    {
        HttpRequestMessage? capturedRequest = null;
        string? payload = null;
        var client = MakeClient(async request =>
        {
            capturedRequest = request;
            payload = await request.Content!.ReadAsStringAsync();
            return JsonContent.Create(new
            {
                id = Guid.Parse("0fc0fb0b-0d1a-4e52-91d0-0b97fb3fe45a"),
                companyName = "Thai Metals Supply",
                taxId = "0105569000001",
                address = "88 Rama IX Road",
                city = "Bangkok",
                country = "Thailand",
                postalCode = "10310",
                status = "Active",
                createdAt = DateTime.UtcNow
            });
        });

        using var response = await client.CreateSupplierAsync(new CreateSupplierRequest
        {
            Name = "Thai Metals Supply",
            TaxId = "0105569000001",
            Email = "sales@thai-metals.example",
            Phone = "+66 2 555 0101",
            Country = "Thailand",
            Address = "88 Rama IX Road",
            City = "Bangkok",
            PostalCode = "10310",
            ContactPerson = "Niran Supplier",
            Website = "https://supplier.example",
            Capabilities = ["Aluminium", "CNC"]
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/supplier/v1/suppliers", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(payload);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.Equal("Thai Metals Supply", root.GetProperty("companyName").GetString());
        Assert.Equal("0105569000001", root.GetProperty("taxId").GetString());
        Assert.Equal("88 Rama IX Road", root.GetProperty("address").GetString());
        Assert.Equal("Bangkok", root.GetProperty("city").GetString());
        Assert.Equal("Thailand", root.GetProperty("country").GetString());
        Assert.Equal("10310", root.GetProperty("postalCode").GetString());
        Assert.Equal("Aluminium", root.GetProperty("capabilities")[0].GetString());
        Assert.Equal("CNC", root.GetProperty("capabilities")[1].GetString());

        var primaryContact = root.GetProperty("primaryContact");
        Assert.Equal("Niran Supplier", primaryContact.GetProperty("name").GetString());
        Assert.Equal("sales@thai-metals.example", primaryContact.GetProperty("email").GetString());
        Assert.Equal("Primary", primaryContact.GetProperty("role").GetString());
        Assert.Equal("+66 2 555 0101", primaryContact.GetProperty("phone").GetString());
        Assert.True(primaryContact.GetProperty("isPrimary").GetBoolean());
    }

    [Fact]
    public async Task GetSupplierByIdAsync_MapsSupplierServiceDetailToIntranetSupplierDetail()
    {
        HttpRequestMessage? capturedRequest = null;
        var supplierId = Guid.Parse("651cce38-3571-4f34-8355-b43d70586dbd");
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return Task.FromResult<HttpContent>(JsonContent.Create(new
            {
                id = supplierId,
                companyName = "Thai Metals Supply",
                taxId = "0105569000001",
                address = "88 Rama IX Road",
                city = "Bangkok",
                country = "Thailand",
                postalCode = "10310",
                status = "Active",
                onboardingStage = "DocumentationReview",
                rowVersion = "42",
                contacts = new[]
                {
                    new
                    {
                        name = "Niran Supplier",
                        email = "sales@thai-metals.example",
                        phoneNumber = "+66 2 555 0101"
                    }
                },
                certifications = new[]
                {
                    new
                    {
                        id = Guid.Parse("08f47199-6f3d-4d54-bb1c-803af7674231"),
                        documentType = "BusinessLicense",
                        documentName = "Business License 2026",
                        issueDate = "2026-01-01",
                        expirationDate = "2027-01-01",
                        externalFileRef = "supplier-documents/license.pdf",
                        isExpired = false,
                        isExpiringSoon = false,
                        createdAt = DateTime.UtcNow
                    }
                },
                capabilities = new[]
                {
                    new { name = "CNC", isActive = true },
                    new { name = "Paused capability", isActive = false }
                },
                performanceSummary = new { overallRating = 4.5m },
                createdAt = DateTime.UtcNow
            }));
        });

        var detail = await client.GetSupplierByIdAsync(supplierId);

        Assert.NotNull(detail);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal($"/supplier/v1/suppliers/{supplierId}", capturedRequest.RequestUri!.PathAndQuery);
        Assert.Equal(supplierId, detail.Id);
        Assert.Equal("Thai Metals Supply", detail.Name);
        Assert.Equal("0105569000001", detail.TaxId);
        Assert.Equal("88 Rama IX Road", detail.Address);
        Assert.Equal("Bangkok", detail.City);
        Assert.Equal("Thailand", detail.Country);
        Assert.Equal("10310", detail.PostalCode);
        Assert.Equal("42", detail.RowVersion);
        Assert.Equal("DocumentationReview", detail.OnboardingStage);
        Assert.Equal("Niran Supplier", detail.ContactPerson);
        Assert.Equal("sales@thai-metals.example", detail.Email);
        Assert.Equal("+66 2 555 0101", detail.Phone);
        Assert.Equal(4.5m, detail.Rating);
        var capability = Assert.Single(detail.Capabilities);
        Assert.Equal("CNC", capability);
        var document = Assert.Single(detail.Documents);
        Assert.Equal("BusinessLicense", document.DocumentType);
        Assert.Equal("Business License 2026", document.DocumentName);
        Assert.Equal("supplier-documents/license.pdf", document.ExternalFileRef);
    }

    [Fact]
    public async Task UpdateSupplierAsync_MapsIntranetSupplierRequestToSupplierServiceConcurrencyContract()
    {
        HttpRequestMessage? capturedRequest = null;
        string? payload = null;
        var supplierId = Guid.Parse("bc0591e2-89f9-4f61-a756-76042d9a49c3");
        var client = MakeClient(async request =>
        {
            capturedRequest = request;
            payload = await request.Content!.ReadAsStringAsync();
            return JsonContent.Create(new
            {
                id = supplierId,
                companyName = "Thai Metals Supply Revised",
                rowVersion = "43"
            });
        });

        using var response = await client.UpdateSupplierAsync(supplierId, new UpdateSupplierRequest
        {
            Name = "Thai Metals Supply Revised",
            Address = "99 Revised Road",
            City = "Bangkok",
            Country = "Thailand",
            PostalCode = "10260",
            Capabilities = ["CNC", "Anodizing"],
            RowVersion = "42"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Put, capturedRequest.Method);
        Assert.Equal($"/supplier/v1/suppliers/{supplierId}", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(payload);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.Equal("Thai Metals Supply Revised", root.GetProperty("companyName").GetString());
        Assert.Equal("99 Revised Road", root.GetProperty("address").GetString());
        Assert.Equal("Bangkok", root.GetProperty("city").GetString());
        Assert.Equal("Thailand", root.GetProperty("country").GetString());
        Assert.Equal("10260", root.GetProperty("postalCode").GetString());
        Assert.Equal("CNC", root.GetProperty("capabilities")[0].GetString());
        Assert.Equal("Anodizing", root.GetProperty("capabilities")[1].GetString());
        Assert.Equal("42", root.GetProperty("rowVersion").GetString());
    }

    [Fact]
    public async Task UpdateSupplierStatusAsync_MapsStatusAndRowVersionToSupplierServiceContract()
    {
        HttpRequestMessage? capturedRequest = null;
        string? payload = null;
        var supplierId = Guid.Parse("4c2f4c8d-fbc8-4e84-8565-5c378c6da726");
        var client = MakeClient(async request =>
        {
            capturedRequest = request;
            payload = await request.Content!.ReadAsStringAsync();
            return JsonContent.Create(new
            {
                id = supplierId,
                companyName = "Thai Metals Supply",
                status = "Active",
                rowVersion = "43"
            });
        });

        using var response = await client.UpdateSupplierStatusAsync(supplierId, new UpdateSupplierStatusRequest
        {
            Status = "Active",
            Reason = "Approved supplier after document review",
            RowVersion = "42"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Patch, capturedRequest.Method);
        Assert.Equal($"/supplier/v1/suppliers/{supplierId}/status", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(payload);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.Equal("Active", root.GetProperty("status").GetString());
        Assert.Equal("Approved supplier after document review", root.GetProperty("reason").GetString());
        Assert.Equal("42", root.GetProperty("rowVersion").GetString());
    }

    [Fact]
    public async Task AddSupplierDocumentAsync_MapsIntranetDocumentToSupplierCertificationContract()
    {
        HttpRequestMessage? capturedRequest = null;
        string? payload = null;
        var supplierId = Guid.Parse("40543057-6f32-46aa-8022-f28a8a99d490");
        var client = MakeClient(async request =>
        {
            capturedRequest = request;
            payload = await request.Content!.ReadAsStringAsync();
            return JsonContent.Create(new
            {
                id = Guid.Parse("6472f408-0759-483c-b69a-6c261bb435a2"),
                documentType = "TaxForm",
                documentName = "Tax Form 2026",
                issueDate = "2026-01-01",
                expirationDate = "2026-12-31",
                externalFileRef = "supplier-documents/tax-form.pdf",
                isExpired = false,
                isExpiringSoon = false,
                createdAt = DateTime.UtcNow
            });
        });

        using var response = await client.AddSupplierDocumentAsync(supplierId, new CreateSupplierDocumentRequest
        {
            DocumentType = "TaxForm",
            DocumentName = "Tax Form 2026",
            IssueDate = new DateOnly(2026, 1, 1),
            ExpirationDate = new DateOnly(2026, 12, 31),
            ExternalFileRef = "supplier-documents/tax-form.pdf",
            Notes = "Verified from supplier onboarding"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal($"/supplier/v1/suppliers/{supplierId}/certifications", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(payload);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.Equal("TaxForm", root.GetProperty("documentType").GetString());
        Assert.Equal("Tax Form 2026", root.GetProperty("documentName").GetString());
        Assert.Equal("2026-01-01", root.GetProperty("issueDate").GetString());
        Assert.Equal("2026-12-31", root.GetProperty("expirationDate").GetString());
        Assert.Equal("supplier-documents/tax-form.pdf", root.GetProperty("externalFileRef").GetString());
        Assert.Equal("Verified from supplier onboarding", root.GetProperty("notes").GetString());
    }

    private static SupplierServiceClient MakeClient(Func<HttpRequestMessage, Task<HttpContent>> contentFactory)
    {
        var handler = new MockHttpMessageHandler(async (request, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = await contentFactory(request)
            });

        return new SupplierServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        });
    }
}

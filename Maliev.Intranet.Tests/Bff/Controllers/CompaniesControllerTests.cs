using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class CompaniesControllerTests
{
    private readonly Mock<CustomerServiceClient> _customerClientMock;
    private readonly Mock<RegistryServiceClient> _registryClientMock;
    private readonly CompaniesController _controller;

    public CompaniesControllerTests()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        _customerClientMock = new Mock<CustomerServiceClient>(
            httpClient,
            new Mock<ILogger<CustomerServiceClient>>().Object);
        _registryClientMock = new Mock<RegistryServiceClient>(httpClient);
        _controller = new CompaniesController(
            _customerClientMock.Object,
            _registryClientMock.Object,
            new Mock<ILogger<CompaniesController>>().Object);
    }

    [Fact]
    public async Task Search_ShouldReturnRegistryCompanies_WhenInternalSearchHasNoMatches()
    {
        _customerClientMock.Setup(client => client.SearchCompanyResultsAsync("มาลี", 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _registryClientMock.Setup(client => client.SearchCompaniesAsync("มาลี", 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new RegistryCompanyProfile
                {
                    TaxId = "0125561001573",
                    CompanyNameTh = "มาลีฟ จำกัด",
                    FullNameTh = "มาลีฟ จำกัด",
                    StatusNameTh = "ยังดำเนินกิจการอยู่"
                }
            ]);

        var result = await _controller.Search("มาลี", 8, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var companies = Assert.IsType<List<CompanySearchResultDto>>(okResult.Value);
        var company = Assert.Single(companies);
        Assert.Equal("มาลีฟ จำกัด", company.Name);
        Assert.Equal("0125561001573", company.VatNumber);
        Assert.Equal("Registry", company.Source);
    }

    [Fact]
    public async Task Search_ShouldPreferInternalCompany_WhenRegistryResultHasSameTaxId()
    {
        var companyId = Guid.NewGuid();
        _customerClientMock.Setup(client => client.SearchCompanyResultsAsync("0125561001573", 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new CompanySearchResultDto
                {
                    Id = companyId,
                    Name = "Existing MALIEV",
                    VatNumber = "0125561001573",
                    Source = "Internal"
                }
            ]);
        _registryClientMock.Setup(client => client.SearchCompaniesAsync("0125561001573", 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new RegistryCompanyProfile
                {
                    TaxId = "0125561001573",
                    CompanyNameTh = "มาลีฟ จำกัด",
                    FullNameTh = "มาลีฟ จำกัด"
                }
            ]);

        var result = await _controller.Search("0125561001573", 8, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var companies = Assert.IsType<List<CompanySearchResultDto>>(okResult.Value);
        var company = Assert.Single(companies);
        Assert.Equal(companyId, company.Id);
        Assert.Equal("Existing MALIEV", company.Name);
        Assert.Equal("Internal", company.Source);
    }
}

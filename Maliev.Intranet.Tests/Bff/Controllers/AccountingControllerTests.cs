using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class AccountingControllerTests
{
    private readonly Mock<IAccountingServiceClient> _clientMock;
    private readonly AccountingController _controller;

    public AccountingControllerTests()
    {
        _clientMock = new Mock<IAccountingServiceClient>();
        _controller = new AccountingController(_clientMock.Object);
    }

    [Fact]
    public async Task GetAccountsTree_ShouldReturnOk_WhenClientReturnsData()
    {
        var data = new List<ChartOfAccountDto> { new() { Code = "1000", Name = "Cash" } };
        _clientMock.Setup(x => x.GetAccountsTreeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var result = await _controller.GetAccountsTree(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(data, okResult.Value);
    }

    [Fact]
    public async Task GetAccountsTree_ShouldReturnEmptyList_WhenClientReturnsNull()
    {
        _clientMock.Setup(x => x.GetAccountsTreeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ChartOfAccountDto>?)null);

        var result = await _controller.GetAccountsTree(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<ChartOfAccountDto>>(okResult.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetJournalEntries_ShouldReturnData()
    {
        var response = new PagedResponse<JournalEntryDto> { Data = new List<JournalEntryDto>() };
        _clientMock.Setup(x => x.GetJournalEntriesAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetJournalEntries(1, 20, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task CreateJournalEntry_ShouldReturnOk_WhenSuccessful()
    {
        var request = new CreateJournalEntryRequest();
        var response = new JournalEntryDto();
        _clientMock.Setup(x => x.CreateJournalEntryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.CreateJournalEntry(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task CreateJournalEntry_ShouldReturnBadRequest_WhenFailed()
    {
        var request = new CreateJournalEntryRequest();
        _clientMock.Setup(x => x.CreateJournalEntryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JournalEntryDto?)null);

        var result = await _controller.CreateJournalEntry(request, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }
}

using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class ProcurementControllerTests
{
    [Fact]
    public async Task Get_ForwardsPurchaseOrderFiltersAndPagination()
    {
        var client = new Mock<IPurchaseOrderServiceClient>();
        client.Setup(c => c.GetPurchaseOrdersAsync(
                "Pending",
                "External",
                7,
                42,
                null,
                null,
                "createdAt",
                "desc",
                2,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<PurchaseOrderDto>
            {
                Data = [new PurchaseOrderDto { Id = 1001, PoNumber = "PO-1001" }],
                Meta = new PaginationMeta { CurrentPage = 2, PageSize = 10, TotalItems = 1, TotalCount = 1, TotalPages = 1 }
            });

        var controller = new ProcurementController(client.Object);

        var result = await controller.Get(
            status: "Pending",
            orderType: "External",
            supplierId: 7,
            orderId: 42,
            page: 2,
            pageSize: 10);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PagedResponse<PurchaseOrderDto>>(ok.Value);
        Assert.Single(response.Data);
    }

    [Fact]
    public async Task GetById_UsesIntPurchaseOrderId()
    {
        var client = new Mock<IPurchaseOrderServiceClient>();
        client.Setup(c => c.GetPurchaseOrderByIdAsync(1001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurchaseOrderDto { Id = 1001, PoNumber = "PO-1001" });

        var controller = new ProcurementController(client.Object);

        var result = await controller.GetById(1001, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var order = Assert.IsType<PurchaseOrderDto>(ok.Value);
        Assert.Equal(1001, order.Id);
    }

    [Fact]
    public async Task Cancel_ForwardsReasonToPurchaseOrderService()
    {
        var client = new Mock<IPurchaseOrderServiceClient>();
        client.Setup(c => c.CancelPurchaseOrderAsync(
                1001,
                It.Is<CancelPurchaseOrderRequest>(request => request.Reason == "Duplicate"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, 204));

        var controller = new ProcurementController(client.Object);

        var result = await controller.Cancel(1001, new CancelPurchaseOrderRequest { Reason = "Duplicate" }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}

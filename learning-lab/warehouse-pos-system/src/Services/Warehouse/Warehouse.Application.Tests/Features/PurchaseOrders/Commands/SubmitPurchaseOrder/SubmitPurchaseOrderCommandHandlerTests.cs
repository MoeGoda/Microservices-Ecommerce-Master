using Common.Exceptions;
using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.PurchaseOrders.Commands.SubmitPurchaseOrder;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.PurchaseOrders.Commands.SubmitPurchaseOrder
{
    public class SubmitPurchaseOrderCommandHandlerTests
    {
        private static PurchaseOrder BuildOrder(PurchaseOrderStatus status) => new()
        {
            Id = 1,
            OrderNumber = "PO-000001",
            SupplierId = 1,
            Supplier = TestEntities.Supplier(),
            Status = status,
        };

        [Fact]
        public async Task Handle_DraftOrder_MovesToOrderedAndStampsOrderedAtUtc()
        {
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var order = BuildOrder(PurchaseOrderStatus.Draft);

            purchaseOrderRepository.Setup(r => r.GetById(order.Id)).ReturnsAsync(order);

            var handler = new SubmitPurchaseOrderCommandHandler(purchaseOrderRepository.Object, unitOfWork.Object);
            var result = await handler.Handle(new SubmitPurchaseOrderCommand { PurchaseOrderId = order.Id }, CancellationToken.None);

            Assert.Equal("Ordered", result.Status);
            Assert.NotNull(order.OrderedAtUtc);
            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Theory]
        [InlineData(PurchaseOrderStatus.Ordered)]
        [InlineData(PurchaseOrderStatus.PartiallyReceived)]
        [InlineData(PurchaseOrderStatus.Received)]
        [InlineData(PurchaseOrderStatus.Cancelled)]
        public async Task Handle_OrderNotInDraft_ThrowsConflictException(PurchaseOrderStatus status)
        {
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var order = BuildOrder(status);

            purchaseOrderRepository.Setup(r => r.GetById(order.Id)).ReturnsAsync(order);

            var handler = new SubmitPurchaseOrderCommandHandler(purchaseOrderRepository.Object, unitOfWork.Object);

            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(new SubmitPurchaseOrderCommand { PurchaseOrderId = order.Id }, CancellationToken.None));

            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_UnknownPurchaseOrder_ThrowsNotFoundException()
        {
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            purchaseOrderRepository.Setup(r => r.GetById(999)).ReturnsAsync((PurchaseOrder?)null);

            var handler = new SubmitPurchaseOrderCommandHandler(purchaseOrderRepository.Object, unitOfWork.Object);

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(new SubmitPurchaseOrderCommand { PurchaseOrderId = 999 }, CancellationToken.None));
        }
    }
}

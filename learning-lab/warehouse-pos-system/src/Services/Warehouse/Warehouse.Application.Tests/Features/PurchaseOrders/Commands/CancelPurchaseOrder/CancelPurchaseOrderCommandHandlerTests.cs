using Common.Exceptions;
using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.PurchaseOrders.Commands.CancelPurchaseOrder;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.PurchaseOrders.Commands.CancelPurchaseOrder
{
    public class CancelPurchaseOrderCommandHandlerTests
    {
        private static PurchaseOrder BuildOrder(PurchaseOrderStatus status) => new()
        {
            Id = 1,
            OrderNumber = "PO-000001",
            SupplierId = 1,
            Supplier = TestEntities.Supplier(),
            Status = status,
        };

        [Theory]
        [InlineData(PurchaseOrderStatus.Draft)]
        [InlineData(PurchaseOrderStatus.Ordered)]
        public async Task Handle_DraftOrOrderedWithNothingReceived_CancelsSuccessfully(PurchaseOrderStatus status)
        {
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var order = BuildOrder(status);

            purchaseOrderRepository.Setup(r => r.GetById(order.Id)).ReturnsAsync(order);

            var handler = new CancelPurchaseOrderCommandHandler(purchaseOrderRepository.Object, unitOfWork.Object);
            var result = await handler.Handle(new CancelPurchaseOrderCommand { PurchaseOrderId = order.Id }, CancellationToken.None);

            Assert.Equal("Cancelled", result.Status);
            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // Once ANY line receives a quantity, ReceivePurchaseOrderLineCommandHandler
        // already moves Status off Ordered (to PartiallyReceived/Received) —
        // so this handler only needs to check for Draft/Ordered, never a
        // separate "has anything been received" query, and this is exactly
        // the case that proves that's sufficient.
        [Theory]
        [InlineData(PurchaseOrderStatus.PartiallyReceived)]
        [InlineData(PurchaseOrderStatus.Received)]
        [InlineData(PurchaseOrderStatus.Cancelled)]
        public async Task Handle_OrderAlreadyReceivedOrCancelled_ThrowsConflictException(PurchaseOrderStatus status)
        {
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var order = BuildOrder(status);

            purchaseOrderRepository.Setup(r => r.GetById(order.Id)).ReturnsAsync(order);

            var handler = new CancelPurchaseOrderCommandHandler(purchaseOrderRepository.Object, unitOfWork.Object);

            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(new CancelPurchaseOrderCommand { PurchaseOrderId = order.Id }, CancellationToken.None));

            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_UnknownPurchaseOrder_ThrowsNotFoundException()
        {
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            purchaseOrderRepository.Setup(r => r.GetById(999)).ReturnsAsync((PurchaseOrder?)null);

            var handler = new CancelPurchaseOrderCommandHandler(purchaseOrderRepository.Object, unitOfWork.Object);

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(new CancelPurchaseOrderCommand { PurchaseOrderId = 999 }, CancellationToken.None));
        }
    }
}

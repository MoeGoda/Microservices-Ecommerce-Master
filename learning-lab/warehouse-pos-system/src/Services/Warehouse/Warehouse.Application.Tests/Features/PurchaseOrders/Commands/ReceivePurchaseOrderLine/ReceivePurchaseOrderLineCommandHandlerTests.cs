using Common.Exceptions;
using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.PurchaseOrders.Commands.ReceivePurchaseOrderLine;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.PurchaseOrders.Commands.ReceivePurchaseOrderLine
{
    public class ReceivePurchaseOrderLineCommandHandlerTests
    {
        private static PurchaseOrder BuildOrder(PurchaseOrderStatus status, PurchaseOrderLine line)
        {
            var order = new PurchaseOrder
            {
                Id = 1,
                OrderNumber = "PO-000001",
                SupplierId = 1,
                Supplier = TestEntities.Supplier(),
                Status = status,
            };
            line.PurchaseOrder = order;
            line.PurchaseOrderId = order.Id;
            order.Lines.Add(line);
            return order;
        }

        [Fact]
        public async Task Handle_OrderNotOrderedOrPartiallyReceived_ThrowsConflictException()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var item = TestEntities.Item(baseUnit: pcs);
            var line = new PurchaseOrderLine { Id = 1, Item = item, ItemId = item.Id, UnitOfMeasure = pcs, UnitOfMeasureId = pcs.Id, OrderedQuantity = 10 };
            var order = BuildOrder(PurchaseOrderStatus.Draft, line);

            purchaseOrderRepository.Setup(r => r.GetById(order.Id)).ReturnsAsync(order);

            var handler = new ReceivePurchaseOrderLineCommandHandler(purchaseOrderRepository.Object, itemUnitRepository.Object, repos.BuildStager(), unitOfWork.Object);
            var command = new ReceivePurchaseOrderLineCommand { PurchaseOrderId = order.Id, PurchaseOrderLineId = line.Id, LocationId = 1, Quantity = 5 };

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_QuantityExceedsRemainingBalanceOnLine_ThrowsConflictException()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var item = TestEntities.Item(baseUnit: pcs);
            var line = new PurchaseOrderLine { Id = 1, Item = item, ItemId = item.Id, UnitOfMeasure = pcs, UnitOfMeasureId = pcs.Id, OrderedQuantity = 10, ReceivedQuantity = 7 };
            var order = BuildOrder(PurchaseOrderStatus.Ordered, line);

            purchaseOrderRepository.Setup(r => r.GetById(order.Id)).ReturnsAsync(order);

            var handler = new ReceivePurchaseOrderLineCommandHandler(purchaseOrderRepository.Object, itemUnitRepository.Object, repos.BuildStager(), unitOfWork.Object);
            // Only 3 remain (10 - 7); asking for 4 must be rejected, not silently capped.
            var command = new ReceivePurchaseOrderLineCommand { PurchaseOrderId = order.Id, PurchaseOrderLineId = line.Id, LocationId = 1, Quantity = 4 };

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_PartialReceipt_IncrementsReceivedQuantityAndMovesStatusToPartiallyReceived()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var item = TestEntities.Item(baseUnit: pcs);
            var location = TestEntities.Location();
            var line = new PurchaseOrderLine { Id = 1, Item = item, ItemId = item.Id, UnitOfMeasure = pcs, UnitOfMeasureId = pcs.Id, OrderedQuantity = 10 };
            var order = BuildOrder(PurchaseOrderStatus.Ordered, line);

            purchaseOrderRepository.Setup(r => r.GetById(order.Id)).ReturnsAsync(order);
            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync((StockLevel?)null);

            var handler = new ReceivePurchaseOrderLineCommandHandler(purchaseOrderRepository.Object, itemUnitRepository.Object, repos.BuildStager(), unitOfWork.Object);
            var command = new ReceivePurchaseOrderLineCommand { PurchaseOrderId = order.Id, PurchaseOrderLineId = line.Id, LocationId = location.Id, Quantity = 4 };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(4, line.ReceivedQuantity);
            Assert.Equal("PartiallyReceived", result.Status);
        }

        [Fact]
        public async Task Handle_ReceivingTheRemainderOfEveryLine_MovesStatusToReceived()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var item = TestEntities.Item(baseUnit: pcs);
            var location = TestEntities.Location();
            var line = new PurchaseOrderLine { Id = 1, Item = item, ItemId = item.Id, UnitOfMeasure = pcs, UnitOfMeasureId = pcs.Id, OrderedQuantity = 10, ReceivedQuantity = 6 };
            var order = BuildOrder(PurchaseOrderStatus.PartiallyReceived, line);

            purchaseOrderRepository.Setup(r => r.GetById(order.Id)).ReturnsAsync(order);
            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync((StockLevel?)null);

            var handler = new ReceivePurchaseOrderLineCommandHandler(purchaseOrderRepository.Object, itemUnitRepository.Object, repos.BuildStager(), unitOfWork.Object);
            var command = new ReceivePurchaseOrderLineCommand { PurchaseOrderId = order.Id, PurchaseOrderLineId = line.Id, LocationId = location.Id, Quantity = 4 };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(10, line.ReceivedQuantity);
            Assert.Equal("Received", result.Status);
        }

        [Fact]
        public async Task Handle_LineOrderedInAlternateUnit_ConvertsToBaseUnitBeforeStagingStock()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var box = TestEntities.UnitOfMeasure(2, "BOX");
            var item = TestEntities.Item(baseUnit: pcs);
            var location = TestEntities.Location();
            var itemUnit = TestEntities.ItemUnit(item, box, conversionFactor: 12);
            var line = new PurchaseOrderLine { Id = 1, Item = item, ItemId = item.Id, UnitOfMeasure = box, UnitOfMeasureId = box.Id, OrderedQuantity = 5 };
            var order = BuildOrder(PurchaseOrderStatus.Ordered, line);
            var existingLevel = TestEntities.StockLevel(item, location, quantityOnHand: 0);

            purchaseOrderRepository.Setup(r => r.GetById(order.Id)).ReturnsAsync(order);
            itemUnitRepository.Setup(r => r.GetByItemAndUnit(item.Id, box.Id)).ReturnsAsync(itemUnit);
            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existingLevel);

            var handler = new ReceivePurchaseOrderLineCommandHandler(purchaseOrderRepository.Object, itemUnitRepository.Object, repos.BuildStager(), unitOfWork.Object);
            // 2 BOX at a factor of 12 -> 24 PCS staged into StockLevel.
            var command = new ReceivePurchaseOrderLineCommand { PurchaseOrderId = order.Id, PurchaseOrderLineId = line.Id, LocationId = location.Id, Quantity = 2 };

            await handler.Handle(command, CancellationToken.None);

            Assert.Equal(24, existingLevel.QuantityOnHand);
            Assert.Equal(2, line.ReceivedQuantity);
        }

        [Fact]
        public async Task Handle_UnknownPurchaseOrderLineId_ThrowsNotFoundException()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var purchaseOrderRepository = new Mock<IPurchaseOrderRepository>();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var item = TestEntities.Item(baseUnit: pcs);
            var line = new PurchaseOrderLine { Id = 1, Item = item, ItemId = item.Id, UnitOfMeasure = pcs, UnitOfMeasureId = pcs.Id, OrderedQuantity = 10 };
            var order = BuildOrder(PurchaseOrderStatus.Ordered, line);

            purchaseOrderRepository.Setup(r => r.GetById(order.Id)).ReturnsAsync(order);

            var handler = new ReceivePurchaseOrderLineCommandHandler(purchaseOrderRepository.Object, itemUnitRepository.Object, repos.BuildStager(), unitOfWork.Object);
            var command = new ReceivePurchaseOrderLineCommand { PurchaseOrderId = order.Id, PurchaseOrderLineId = 999, LocationId = 1, Quantity = 1 };

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        }
    }
}

using Common.Exceptions;
using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.PurchaseOrders.Commands.CreatePurchaseOrder
{
    public class CreatePurchaseOrderCommandHandlerTests
    {
        private readonly Mock<ISupplierRepository> _supplierRepository = new();
        private readonly Mock<IItemRepository> _itemRepository = new();
        private readonly Mock<IUnitOfMeasureRepository> _unitOfMeasureRepository = new();
        private readonly Mock<IItemUnitRepository> _itemUnitRepository = new();
        private readonly Mock<IPurchaseOrderRepository> _purchaseOrderRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private CreatePurchaseOrderCommandHandler BuildHandler() => new(
            _supplierRepository.Object,
            _itemRepository.Object,
            _unitOfMeasureRepository.Object,
            _itemUnitRepository.Object,
            _purchaseOrderRepository.Object,
            _unitOfWork.Object);

        [Fact]
        public async Task Handle_UnknownSupplier_ThrowsNotFoundException()
        {
            _supplierRepository.Setup(r => r.GetById(1)).ReturnsAsync((Supplier?)null);

            var command = new CreatePurchaseOrderCommand { SupplierId = 1, CreatedByUserId = 1, Lines = { new() { ItemId = 1, UnitOfMeasureId = 1, OrderedQuantity = 1 } } };

            await Assert.ThrowsAsync<NotFoundException>(() => BuildHandler().Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_DeactivatedSupplier_ThrowsConflictExceptionBeforeTouchingLines()
        {
            var supplier = TestEntities.Supplier(isActive: false);
            _supplierRepository.Setup(r => r.GetById(supplier.Id)).ReturnsAsync(supplier);

            var command = new CreatePurchaseOrderCommand
            {
                SupplierId = supplier.Id,
                CreatedByUserId = 1,
                Lines = { new() { ItemId = 1, UnitOfMeasureId = 1, OrderedQuantity = 1 } },
            };

            await Assert.ThrowsAsync<ConflictException>(() => BuildHandler().Handle(command, CancellationToken.None));

            _itemRepository.Verify(r => r.GetById(It.IsAny<int>()), Times.Never);
        }

        // The README's "L — A real Purchase Order bug" regression: a line
        // ordered in a unit the item has no ItemUnit conversion for (and
        // isn't the item's own base unit) used to sail through Create and
        // Submit, only failing much later at Receive. This asserts Create
        // itself now rejects it.
        [Fact]
        public async Task Handle_LineUnitHasNoConversionForItemAndIsNotBaseUnit_ThrowsConflictExceptionAtCreateTime()
        {
            var supplier = TestEntities.Supplier();
            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var box = TestEntities.UnitOfMeasure(2, "BOX");
            var item = TestEntities.Item(baseUnit: pcs);

            _supplierRepository.Setup(r => r.GetById(supplier.Id)).ReturnsAsync(supplier);
            _itemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            _unitOfMeasureRepository.Setup(r => r.GetById(box.Id)).ReturnsAsync(box);
            _itemUnitRepository.Setup(r => r.GetByItemAndUnit(item.Id, box.Id)).ReturnsAsync((ItemUnit?)null);

            var command = new CreatePurchaseOrderCommand
            {
                SupplierId = supplier.Id,
                CreatedByUserId = 1,
                Lines = { new() { ItemId = item.Id, UnitOfMeasureId = box.Id, OrderedQuantity = 10 } },
            };

            await Assert.ThrowsAsync<ConflictException>(() => BuildHandler().Handle(command, CancellationToken.None));

            _purchaseOrderRepository.Verify(r => r.AddAsync(It.IsAny<PurchaseOrder>()), Times.Never);
        }

        [Fact]
        public async Task Handle_LineUnitIsItemsOwnBaseUnit_SucceedsWithNoConversionLookup()
        {
            var supplier = TestEntities.Supplier();
            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var item = TestEntities.Item(baseUnit: pcs);

            _supplierRepository.Setup(r => r.GetById(supplier.Id)).ReturnsAsync(supplier);
            _itemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            _unitOfMeasureRepository.Setup(r => r.GetById(pcs.Id)).ReturnsAsync(pcs);
            _purchaseOrderRepository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>()))
                .ReturnsAsync((PurchaseOrder o) => { o.Id = 7; return o; });

            var command = new CreatePurchaseOrderCommand
            {
                SupplierId = supplier.Id,
                CreatedByUserId = 1,
                Lines = { new() { ItemId = item.Id, UnitOfMeasureId = pcs.Id, OrderedQuantity = 10, UnitCost = 2.5m } },
            };

            var result = await BuildHandler().Handle(command, CancellationToken.None);

            Assert.Equal("PO-000007", result.OrderNumber);
            _itemUnitRepository.Verify(r => r.GetByItemAndUnit(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_LineUnitHasAValidConversion_Succeeds()
        {
            var supplier = TestEntities.Supplier();
            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var box = TestEntities.UnitOfMeasure(2, "BOX");
            var item = TestEntities.Item(baseUnit: pcs);
            var itemUnit = TestEntities.ItemUnit(item, box, conversionFactor: 12);

            _supplierRepository.Setup(r => r.GetById(supplier.Id)).ReturnsAsync(supplier);
            _itemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            _unitOfMeasureRepository.Setup(r => r.GetById(box.Id)).ReturnsAsync(box);
            _itemUnitRepository.Setup(r => r.GetByItemAndUnit(item.Id, box.Id)).ReturnsAsync(itemUnit);
            _purchaseOrderRepository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>()))
                .ReturnsAsync((PurchaseOrder o) => { o.Id = 1; return o; });

            var command = new CreatePurchaseOrderCommand
            {
                SupplierId = supplier.Id,
                CreatedByUserId = 1,
                Lines = { new() { ItemId = item.Id, UnitOfMeasureId = box.Id, OrderedQuantity = 5, UnitCost = 30m } },
            };

            var result = await BuildHandler().Handle(command, CancellationToken.None);

            Assert.Single(result.Lines);
            Assert.Equal(5, result.Lines[0].OrderedQuantity);
        }

        [Fact]
        public async Task Handle_OrderNumberAssignedFromRealIdAfterFirstSave_AndSavedASecondTime()
        {
            var supplier = TestEntities.Supplier();
            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var item = TestEntities.Item(baseUnit: pcs);

            _supplierRepository.Setup(r => r.GetById(supplier.Id)).ReturnsAsync(supplier);
            _itemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            _unitOfMeasureRepository.Setup(r => r.GetById(pcs.Id)).ReturnsAsync(pcs);
            _purchaseOrderRepository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>()))
                .ReturnsAsync((PurchaseOrder o) => { o.Id = 42; return o; });

            var command = new CreatePurchaseOrderCommand
            {
                SupplierId = supplier.Id,
                CreatedByUserId = 1,
                Lines = { new() { ItemId = item.Id, UnitOfMeasureId = pcs.Id, OrderedQuantity = 1 } },
            };

            var result = await BuildHandler().Handle(command, CancellationToken.None);

            Assert.Equal("PO-000042", result.OrderNumber);
            _purchaseOrderRepository.Verify(r => r.UpdateAsync(It.Is<PurchaseOrder>(o => o.OrderNumber == "PO-000042")), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        }
    }
}

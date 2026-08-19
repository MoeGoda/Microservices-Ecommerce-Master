using Common.Exceptions;
using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.Outbox;
using Warehouse.Application.Features.Stock.Commands.ReceiveStock;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.Stock.Commands.ReceiveStock
{
    public class ReceiveStockCommandHandlerTests
    {
        private static ReceiveStockCommandHandler BuildHandler(
            StagedRepositories repos,
            Mock<IItemUnitRepository> itemUnitRepository,
            Mock<IUnitOfMeasureRepository> unitOfMeasureRepository,
            Mock<IUnitOfWork> unitOfWork)
        {
            return new ReceiveStockCommandHandler(
                repos.ItemRepository.Object,
                itemUnitRepository.Object,
                unitOfMeasureRepository.Object,
                repos.BuildStager(),
                unitOfWork.Object);
        }

        [Fact]
        public async Task Handle_ReceivingInBaseUnit_AddsQuantityDirectlyWithNoConversion()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfMeasureRepository = new Mock<IUnitOfMeasureRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var item = TestEntities.Item(baseUnit: pcs);
            var location = TestEntities.Location();
            var existing = TestEntities.StockLevel(item, location, quantityOnHand: 10);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existing);
            unitOfMeasureRepository.Setup(r => r.GetById(pcs.Id)).ReturnsAsync(pcs);

            var handler = BuildHandler(repos, itemUnitRepository, unitOfMeasureRepository, unitOfWork);
            var command = new ReceiveStockCommand { ItemId = item.Id, LocationId = location.Id, Quantity = 10, UnitOfMeasureId = pcs.Id };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(20, result.QuantityOnHand);
        }

        [Fact]
        public async Task Handle_ReceivingInAlternateUnit_ConvertsThroughConversionFactorBeforeTouchingStock()
        {
            // 2 CARTON at a factor of 24 -> 48 PCS added, matching B2's
            // own "receive 10 PCS then 2 CARTON (2x24) -> 58" example.
            var repos = StockAdjustmentStagerTestFactory.Create();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfMeasureRepository = new Mock<IUnitOfMeasureRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var carton = TestEntities.UnitOfMeasure(2, "CARTON");
            var item = TestEntities.Item(baseUnit: pcs);
            var location = TestEntities.Location();
            var existing = TestEntities.StockLevel(item, location, quantityOnHand: 10);
            var itemUnit = TestEntities.ItemUnit(item, carton, conversionFactor: 24);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existing);
            unitOfMeasureRepository.Setup(r => r.GetById(carton.Id)).ReturnsAsync(carton);
            itemUnitRepository.Setup(r => r.GetByItemAndUnit(item.Id, carton.Id)).ReturnsAsync(itemUnit);

            var handler = BuildHandler(repos, itemUnitRepository, unitOfMeasureRepository, unitOfWork);
            var command = new ReceiveStockCommand { ItemId = item.Id, LocationId = location.Id, Quantity = 2, UnitOfMeasureId = carton.Id };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(58, result.QuantityOnHand);
        }

        [Fact]
        public async Task Handle_ConversionThatDoesNotLandOnWholeNumber_ThrowsConflictExceptionRatherThanRounding()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfMeasureRepository = new Mock<IUnitOfMeasureRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var oddUnit = TestEntities.UnitOfMeasure(2, "ODD");
            var item = TestEntities.Item(baseUnit: pcs);
            var location = TestEntities.Location();
            var itemUnit = TestEntities.ItemUnit(item, oddUnit, conversionFactor: 2.5m);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            unitOfMeasureRepository.Setup(r => r.GetById(oddUnit.Id)).ReturnsAsync(oddUnit);
            itemUnitRepository.Setup(r => r.GetByItemAndUnit(item.Id, oddUnit.Id)).ReturnsAsync(itemUnit);

            var handler = BuildHandler(repos, itemUnitRepository, unitOfMeasureRepository, unitOfWork);
            // 1 * 2.5 = 2.5, not a whole number.
            var command = new ReceiveStockCommand { ItemId = item.Id, LocationId = location.Id, Quantity = 1, UnitOfMeasureId = oddUnit.Id };

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_UnitWithNoConversionSetUpForItem_ThrowsNotFoundException()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfMeasureRepository = new Mock<IUnitOfMeasureRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var box = TestEntities.UnitOfMeasure(2, "BOX");
            var item = TestEntities.Item(baseUnit: pcs);
            var location = TestEntities.Location();

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            unitOfMeasureRepository.Setup(r => r.GetById(box.Id)).ReturnsAsync(box);
            itemUnitRepository.Setup(r => r.GetByItemAndUnit(item.Id, box.Id)).ReturnsAsync((ItemUnit?)null);

            var handler = BuildHandler(repos, itemUnitRepository, unitOfMeasureRepository, unitOfWork);
            var command = new ReceiveStockCommand { ItemId = item.Id, LocationId = location.Id, Quantity = 1, UnitOfMeasureId = box.Id };

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_FirstEverReceiptAtLocation_CreatesStockLevelInsteadOfThrowing()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfMeasureRepository = new Mock<IUnitOfMeasureRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var item = TestEntities.Item(baseUnit: pcs);
            var location = TestEntities.Location();

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync((StockLevel?)null);
            unitOfMeasureRepository.Setup(r => r.GetById(pcs.Id)).ReturnsAsync(pcs);

            var handler = BuildHandler(repos, itemUnitRepository, unitOfMeasureRepository, unitOfWork);
            var command = new ReceiveStockCommand { ItemId = item.Id, LocationId = location.Id, Quantity = 15, UnitOfMeasureId = pcs.Id };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(15, result.QuantityOnHand);
            repos.StockLevelRepository.Verify(r => r.AddAsync(It.IsAny<StockLevel>()), Times.Once);
        }

        // Regression test for the gap D1/D2 flagged and the README's own
        // "Update" note closed: ReceiveStockCommand used to bypass
        // StockAdjustmentStager entirely (it needed unit conversion first),
        // so a PO/free-text receipt never emitted StockLevelChanged and
        // Reporting/Notifications never heard about it. This asserts the
        // fix actually holds — receiving now stages that event same as
        // AdjustStockCommand/TransferStockCommand do.
        [Fact]
        public async Task Handle_SuccessfulReceipt_EmitsStockLevelChangedEvent()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var itemUnitRepository = new Mock<IItemUnitRepository>();
            var unitOfMeasureRepository = new Mock<IUnitOfMeasureRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var pcs = TestEntities.UnitOfMeasure(1, "PCS");
            var item = TestEntities.Item(baseUnit: pcs);
            var location = TestEntities.Location();
            var existing = TestEntities.StockLevel(item, location, quantityOnHand: 5);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existing);
            unitOfMeasureRepository.Setup(r => r.GetById(pcs.Id)).ReturnsAsync(pcs);

            var handler = BuildHandler(repos, itemUnitRepository, unitOfMeasureRepository, unitOfWork);
            var command = new ReceiveStockCommand { ItemId = item.Id, LocationId = location.Id, Quantity = 10, UnitOfMeasureId = pcs.Id };

            await handler.Handle(command, CancellationToken.None);

            repos.OutboxRepository.Verify(o => o.AddMessageAsync(
                It.Is<OutboxMessage>(m => m.EventType == OutboxEventTypes.StockLevelChanged)), Times.Once);
            repos.OutboxRepository.Verify(o => o.AddDeliveryAsync(
                It.Is<OutboxDelivery>(d => d.ConsumerName == OutboxConsumers.Reporting)), Times.AtLeastOnce);
            repos.OutboxRepository.Verify(o => o.AddDeliveryAsync(
                It.Is<OutboxDelivery>(d => d.ConsumerName == OutboxConsumers.Notifications)), Times.Once);
            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}

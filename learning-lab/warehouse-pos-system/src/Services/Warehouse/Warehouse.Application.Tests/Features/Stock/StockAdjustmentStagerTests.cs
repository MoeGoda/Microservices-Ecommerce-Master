using Common.Exceptions;
using Moq;
using Warehouse.Application.Exceptions;
using Warehouse.Application.Features.Outbox;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;
using Xunit;

namespace Warehouse.Application.Tests.Features.Stock
{
    public class StockAdjustmentStagerTests
    {
        [Fact]
        public async Task Stage_PositiveChangeOnExistingLevel_IncreasesQuantityOnHand()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var item = TestEntities.Item();
            var location = TestEntities.Location();
            var existing = TestEntities.StockLevel(item, location, quantityOnHand: 50);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existing);

            var stager = repos.BuildStager();

            var result = await stager.Stage(item.Id, location.Id, 20, StockTransactionReason.Received, null);

            Assert.Equal(70, result.StockLevel.QuantityOnHand);
            repos.StockLevelRepository.Verify(r => r.UpdateAsync(existing), Times.Once);
            repos.StockLevelRepository.Verify(r => r.AddAsync(It.IsAny<StockLevel>()), Times.Never);
        }

        [Fact]
        public async Task Stage_NegativeChangeThatWouldGoBelowZero_ThrowsInsufficientStockException()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var item = TestEntities.Item();
            var location = TestEntities.Location();
            var existing = TestEntities.StockLevel(item, location, quantityOnHand: 5);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existing);

            var stager = repos.BuildStager();

            await Assert.ThrowsAsync<InsufficientStockException>(
                () => stager.Stage(item.Id, location.Id, -6, StockTransactionReason.Adjustment, null));

            // Nothing should have been persisted once the guard rejects the change.
            repos.StockLevelRepository.Verify(r => r.UpdateAsync(It.IsAny<StockLevel>()), Times.Never);
            repos.OutboxRepository.Verify(o => o.AddMessageAsync(It.IsAny<OutboxMessage>()), Times.Never);
        }

        [Fact]
        public async Task Stage_NegativeChangeThatLandsExactlyOnZero_Succeeds()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var item = TestEntities.Item();
            var location = TestEntities.Location();
            var existing = TestEntities.StockLevel(item, location, quantityOnHand: 6);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existing);

            var stager = repos.BuildStager();

            var result = await stager.Stage(item.Id, location.Id, -6, StockTransactionReason.Adjustment, null);

            Assert.Equal(0, result.StockLevel.QuantityOnHand);
        }

        [Fact]
        public async Task Stage_NoExistingLevelAndCreateIfMissingFalse_ThrowsNotFoundException()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var item = TestEntities.Item();
            var location = TestEntities.Location();

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync((StockLevel?)null);

            var stager = repos.BuildStager();

            await Assert.ThrowsAsync<NotFoundException>(
                () => stager.Stage(item.Id, location.Id, 5, StockTransactionReason.Adjustment, null, createIfMissing: false));
        }

        [Fact]
        public async Task Stage_NoExistingLevelAndCreateIfMissingTrue_CreatesNewStockLevelAtRequestedQuantity()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var item = TestEntities.Item();
            var location = TestEntities.Location();

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync((StockLevel?)null);

            var stager = repos.BuildStager();

            var result = await stager.Stage(item.Id, location.Id, 30, StockTransactionReason.TransferIn, null, createIfMissing: true);

            Assert.Equal(30, result.StockLevel.QuantityOnHand);
            repos.StockLevelRepository.Verify(r => r.AddAsync(It.IsAny<StockLevel>()), Times.Once);
        }

        [Fact]
        public async Task Stage_UnknownItem_ThrowsNotFoundException()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            repos.ItemRepository.Setup(r => r.GetById(999)).ReturnsAsync((Item?)null);

            var stager = repos.BuildStager();

            await Assert.ThrowsAsync<NotFoundException>(
                () => stager.Stage(999, 1, 5, StockTransactionReason.Adjustment, null));
        }

        [Fact]
        public async Task Stage_UnknownLocation_ThrowsNotFoundException()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var item = TestEntities.Item();
            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(999)).ReturnsAsync((Location?)null);

            var stager = repos.BuildStager();

            await Assert.ThrowsAsync<NotFoundException>(
                () => stager.Stage(item.Id, 999, 5, StockTransactionReason.Adjustment, null));
        }

        [Fact]
        public async Task Stage_SuccessfulChange_EmitsStockLevelChangedToReportingAndNotifications()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var item = TestEntities.Item();
            var location = TestEntities.Location();
            var existing = TestEntities.StockLevel(item, location, quantityOnHand: 10);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existing);

            var stager = repos.BuildStager();

            await stager.Stage(item.Id, location.Id, 5, StockTransactionReason.Received, null);

            repos.OutboxRepository.Verify(o => o.AddMessageAsync(
                It.Is<OutboxMessage>(m => m.EventType == OutboxEventTypes.StockLevelChanged)), Times.Once);
            repos.OutboxRepository.Verify(o => o.AddDeliveryAsync(
                It.Is<OutboxDelivery>(d => d.ConsumerName == OutboxConsumers.Reporting
                    && d.OutboxMessage.EventType == OutboxEventTypes.StockLevelChanged)), Times.Once);
            repos.OutboxRepository.Verify(o => o.AddDeliveryAsync(
                It.Is<OutboxDelivery>(d => d.ConsumerName == OutboxConsumers.Notifications)), Times.Once);
        }

        [Fact]
        public async Task Stage_SuccessfulChange_AlsoEmitsStockTransactionRecordedToReportingOnly()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var item = TestEntities.Item();
            var location = TestEntities.Location();
            var existing = TestEntities.StockLevel(item, location, quantityOnHand: 10);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existing);

            var stager = repos.BuildStager();

            await stager.Stage(item.Id, location.Id, 5, StockTransactionReason.Received, "REF-1");

            repos.OutboxRepository.Verify(o => o.AddMessageAsync(
                It.Is<OutboxMessage>(m => m.EventType == OutboxEventTypes.StockTransactionRecorded)), Times.Once);
            repos.OutboxRepository.Verify(o => o.AddDeliveryAsync(
                It.Is<OutboxDelivery>(d => d.ConsumerName == OutboxConsumers.Notifications)), Times.Once);
        }

        [Fact]
        public async Task Stage_RecordsStockTransactionWithSignedQuantityAndReason()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var item = TestEntities.Item();
            var location = TestEntities.Location();
            var existing = TestEntities.StockLevel(item, location, quantityOnHand: 10);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existing);

            var stager = repos.BuildStager();

            await stager.Stage(item.Id, location.Id, -3, StockTransactionReason.TransferOut, "TRANSFER-1");

            repos.StockTransactionRepository.Verify(r => r.AddAsync(It.Is<StockTransaction>(
                t => t.QuantityChange == -3
                     && t.Reason == StockTransactionReason.TransferOut
                     && t.Reference == "TRANSFER-1")), Times.Once);
        }
    }
}

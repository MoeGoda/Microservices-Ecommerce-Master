using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Exceptions;
using Warehouse.Application.Features.Stock.Commands.ApplySale;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.Stock.Commands.ApplySale
{
    public class ApplySaleCommandHandlerTests
    {
        [Fact]
        public async Task Handle_SaleAlreadyProcessed_ReturnsNoOpWithoutTouchingStock()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var processedSaleEventRepository = new Mock<IProcessedSaleEventRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            processedSaleEventRepository.Setup(r => r.ExistsForSale(42)).ReturnsAsync(true);

            var handler = new ApplySaleCommandHandler(processedSaleEventRepository.Object, repos.BuildStager(), unitOfWork.Object);
            var command = new ApplySaleCommand
            {
                SaleId = 42,
                LocationId = 1,
                Lines = { new ApplySaleLine { ItemId = 1, Quantity = 5 } },
            };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.AlreadyProcessed);
            repos.ItemRepository.Verify(r => r.GetById(It.IsAny<int>()), Times.Never);
            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_NewSaleWithSufficientStock_DecrementsEveryLineAndRecordsProcessedEvent()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var processedSaleEventRepository = new Mock<IProcessedSaleEventRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            processedSaleEventRepository.Setup(r => r.ExistsForSale(42)).ReturnsAsync(false);

            var location = TestEntities.Location();
            var itemA = TestEntities.Item(1, "SKU-A");
            var itemB = TestEntities.Item(2, "SKU-B");
            var levelA = TestEntities.StockLevel(itemA, location, quantityOnHand: 10);
            var levelB = TestEntities.StockLevel(itemB, location, quantityOnHand: 10);

            repos.ItemRepository.Setup(r => r.GetById(itemA.Id)).ReturnsAsync(itemA);
            repos.ItemRepository.Setup(r => r.GetById(itemB.Id)).ReturnsAsync(itemB);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(itemA.Id, location.Id)).ReturnsAsync(levelA);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(itemB.Id, location.Id)).ReturnsAsync(levelB);

            var handler = new ApplySaleCommandHandler(processedSaleEventRepository.Object, repos.BuildStager(), unitOfWork.Object);
            var command = new ApplySaleCommand
            {
                SaleId = 42,
                LocationId = location.Id,
                Lines =
                {
                    new ApplySaleLine { ItemId = itemA.Id, Quantity = 3 },
                    new ApplySaleLine { ItemId = itemB.Id, Quantity = 4 },
                },
            };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.False(result.AlreadyProcessed);
            Assert.Equal(7, levelA.QuantityOnHand);
            Assert.Equal(6, levelB.QuantityOnHand);
            processedSaleEventRepository.Verify(r => r.AddAsync(It.Is<ProcessedSaleEvent>(e => e.SaleId == 42)), Times.Once);
            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_OneLineShortOnStock_ThrowsAndNeverCommitsAnyLine()
        {
            // Atomicity through deferred SaveChangesAsync: if line 2 of 2
            // would go negative, line 1's already-staged decrement must
            // never be saved either — there's no per-line compensation to
            // undo it, so the guard has to fire before the shared commit.
            var repos = StockAdjustmentStagerTestFactory.Create();
            var processedSaleEventRepository = new Mock<IProcessedSaleEventRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            processedSaleEventRepository.Setup(r => r.ExistsForSale(42)).ReturnsAsync(false);

            var location = TestEntities.Location();
            var itemA = TestEntities.Item(1, "SKU-A");
            var itemB = TestEntities.Item(2, "SKU-B");
            var levelA = TestEntities.StockLevel(itemA, location, quantityOnHand: 10);
            var levelB = TestEntities.StockLevel(itemB, location, quantityOnHand: 2);

            repos.ItemRepository.Setup(r => r.GetById(itemA.Id)).ReturnsAsync(itemA);
            repos.ItemRepository.Setup(r => r.GetById(itemB.Id)).ReturnsAsync(itemB);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(itemA.Id, location.Id)).ReturnsAsync(levelA);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(itemB.Id, location.Id)).ReturnsAsync(levelB);

            var handler = new ApplySaleCommandHandler(processedSaleEventRepository.Object, repos.BuildStager(), unitOfWork.Object);
            var command = new ApplySaleCommand
            {
                SaleId = 42,
                LocationId = location.Id,
                Lines =
                {
                    new ApplySaleLine { ItemId = itemA.Id, Quantity = 3 },
                    new ApplySaleLine { ItemId = itemB.Id, Quantity = 5 },
                },
            };

            await Assert.ThrowsAsync<InsufficientStockException>(() => handler.Handle(command, CancellationToken.None));

            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
            processedSaleEventRepository.Verify(r => r.AddAsync(It.IsAny<ProcessedSaleEvent>()), Times.Never);
        }
    }
}

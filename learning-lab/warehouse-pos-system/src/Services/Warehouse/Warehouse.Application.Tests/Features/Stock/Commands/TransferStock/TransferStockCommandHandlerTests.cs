using Common.Exceptions;
using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Exceptions;
using Warehouse.Application.Features.Stock.Commands.TransferStock;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.Stock.Commands.TransferStock
{
    public class TransferStockCommandHandlerTests
    {
        [Fact]
        public async Task Handle_SufficientSourceStock_MovesQuantityFromSourceToDestination()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var unitOfWork = new Mock<IUnitOfWork>();
            var item = TestEntities.Item();
            var source = TestEntities.Location(1, "A1", "Aisle 1");
            var destination = TestEntities.Location(2, "B1", "Aisle 2 back stock");
            var sourceLevel = TestEntities.StockLevel(item, source, quantityOnHand: 50);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(source.Id)).ReturnsAsync(source);
            repos.LocationRepository.Setup(r => r.GetById(destination.Id)).ReturnsAsync(destination);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, source.Id)).ReturnsAsync(sourceLevel);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, destination.Id)).ReturnsAsync((StockLevel?)null);

            var handler = new TransferStockCommandHandler(repos.BuildStager(), unitOfWork.Object);
            var command = new TransferStockCommand { ItemId = item.Id, FromLocationId = source.Id, ToLocationId = destination.Id, Quantity = 20 };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(30, result.From.QuantityOnHand);
            Assert.Equal(20, result.To.QuantityOnHand);
            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_DestinationHasNoExistingStockLevel_CreatesOneRatherThanThrowing()
        {
            // createIfMissing: true at the destination — a transfer can
            // legitimately be the first stock an item has ever had there.
            var repos = StockAdjustmentStagerTestFactory.Create();
            var unitOfWork = new Mock<IUnitOfWork>();
            var item = TestEntities.Item();
            var source = TestEntities.Location(1, "A1", "Aisle 1");
            var destination = TestEntities.Location(2, "B1", "Aisle 2 back stock");
            var sourceLevel = TestEntities.StockLevel(item, source, quantityOnHand: 10);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(source.Id)).ReturnsAsync(source);
            repos.LocationRepository.Setup(r => r.GetById(destination.Id)).ReturnsAsync(destination);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, source.Id)).ReturnsAsync(sourceLevel);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, destination.Id)).ReturnsAsync((StockLevel?)null);

            var handler = new TransferStockCommandHandler(repos.BuildStager(), unitOfWork.Object);
            var command = new TransferStockCommand { ItemId = item.Id, FromLocationId = source.Id, ToLocationId = destination.Id, Quantity = 10 };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(10, result.To.QuantityOnHand);
        }

        [Fact]
        public async Task Handle_SourceHasInsufficientStock_ThrowsAndNeverTouchesDestinationOrCommits()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var unitOfWork = new Mock<IUnitOfWork>();
            var item = TestEntities.Item();
            var source = TestEntities.Location(1, "A1", "Aisle 1");
            var destination = TestEntities.Location(2, "B1", "Aisle 2 back stock");
            var sourceLevel = TestEntities.StockLevel(item, source, quantityOnHand: 5);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(source.Id)).ReturnsAsync(source);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, source.Id)).ReturnsAsync(sourceLevel);

            var handler = new TransferStockCommandHandler(repos.BuildStager(), unitOfWork.Object);
            var command = new TransferStockCommand { ItemId = item.Id, FromLocationId = source.Id, ToLocationId = destination.Id, Quantity = 6 };

            await Assert.ThrowsAsync<InsufficientStockException>(() => handler.Handle(command, CancellationToken.None));

            // Source failing first means the destination location is
            // never even looked up — there's no half-transfer state.
            repos.LocationRepository.Verify(r => r.GetById(destination.Id), Times.Never);
            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_SourceHasNoStockLevelAtAll_ThrowsNotFoundException()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var unitOfWork = new Mock<IUnitOfWork>();
            var item = TestEntities.Item();
            var source = TestEntities.Location(1, "A1", "Aisle 1");
            var destination = TestEntities.Location(2, "B1", "Aisle 2 back stock");

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(source.Id)).ReturnsAsync(source);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, source.Id)).ReturnsAsync((StockLevel?)null);

            var handler = new TransferStockCommandHandler(repos.BuildStager(), unitOfWork.Object);
            var command = new TransferStockCommand { ItemId = item.Id, FromLocationId = source.Id, ToLocationId = destination.Id, Quantity = 6 };

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        }
    }
}

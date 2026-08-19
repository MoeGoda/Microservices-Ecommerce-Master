using Common.Exceptions;
using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Exceptions;
using Warehouse.Application.Features.Stock.Commands.AdjustStock;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.Stock.Commands.AdjustStock
{
    public class AdjustStockCommandHandlerTests
    {
        [Fact]
        public async Task Handle_PositiveAdjustment_IncreasesQuantityAndCommitsOnce()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var unitOfWork = new Mock<IUnitOfWork>();
            var item = TestEntities.Item();
            var location = TestEntities.Location();
            var existing = TestEntities.StockLevel(item, location, quantityOnHand: 40);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existing);

            var handler = new AdjustStockCommandHandler(repos.BuildStager(), unitOfWork.Object);
            var command = new AdjustStockCommand { ItemId = item.Id, LocationId = location.Id, QuantityChange = 10 };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(50, result.QuantityOnHand);
            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_AdjustmentThatWouldGoNegative_ThrowsInsufficientStockExceptionAndNeverCommits()
        {
            var repos = StockAdjustmentStagerTestFactory.Create();
            var unitOfWork = new Mock<IUnitOfWork>();
            var item = TestEntities.Item();
            var location = TestEntities.Location();
            var existing = TestEntities.StockLevel(item, location, quantityOnHand: 3);

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync(existing);

            var handler = new AdjustStockCommandHandler(repos.BuildStager(), unitOfWork.Object);
            var command = new AdjustStockCommand { ItemId = item.Id, LocationId = location.Id, QuantityChange = -4 };

            await Assert.ThrowsAsync<InsufficientStockException>(() => handler.Handle(command, CancellationToken.None));

            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_NoExistingStockLevelAtLocation_ThrowsNotFoundInsteadOfCreatingOne()
        {
            // Adjust never creates a StockLevel — unlike Receive/Transfer's
            // createIfMissing: true — because "adjusting" implies a
            // balance already exists to adjust.
            var repos = StockAdjustmentStagerTestFactory.Create();
            var unitOfWork = new Mock<IUnitOfWork>();
            var item = TestEntities.Item();
            var location = TestEntities.Location();

            repos.ItemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            repos.LocationRepository.Setup(r => r.GetById(location.Id)).ReturnsAsync(location);
            repos.StockLevelRepository.Setup(r => r.GetByItemAndLocation(item.Id, location.Id)).ReturnsAsync((StockLevel?)null);

            var handler = new AdjustStockCommandHandler(repos.BuildStager(), unitOfWork.Object);
            var command = new AdjustStockCommand { ItemId = item.Id, LocationId = location.Id, QuantityChange = 5 };

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        }
    }
}

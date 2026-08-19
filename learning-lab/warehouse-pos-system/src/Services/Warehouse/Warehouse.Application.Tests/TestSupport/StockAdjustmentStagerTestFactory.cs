using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.Stock;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.TestSupport
{
    // StockAdjustmentStager is a concrete class every stock-affecting
    // handler constructs directly (not an interface swapped via DI) — so
    // "mock the stager" isn't an option for handler tests. This builds a
    // real one wired to mocked repositories instead, the same shape
    // production DI assembles it with.
    internal sealed class StagedRepositories
    {
        public Mock<IItemRepository> ItemRepository { get; } = new();
        public Mock<ILocationRepository> LocationRepository { get; } = new();
        public Mock<IStockLevelRepository> StockLevelRepository { get; } = new();
        public Mock<IStockTransactionRepository> StockTransactionRepository { get; } = new();
        public Mock<IOutboxRepository> OutboxRepository { get; } = new();

        public StockAdjustmentStager BuildStager() => new(
            ItemRepository.Object,
            LocationRepository.Object,
            StockLevelRepository.Object,
            StockTransactionRepository.Object,
            OutboxRepository.Object);
    }

    internal static class StockAdjustmentStagerTestFactory
    {
        public static StagedRepositories Create()
        {
            var repos = new StagedRepositories();

            repos.OutboxRepository
                .Setup(o => o.AddMessageAsync(It.IsAny<OutboxMessage>()))
                .ReturnsAsync((OutboxMessage m) => m);
            repos.OutboxRepository
                .Setup(o => o.AddDeliveryAsync(It.IsAny<OutboxDelivery>()))
                .ReturnsAsync((OutboxDelivery d) => d);
            repos.StockLevelRepository
                .Setup(s => s.AddAsync(It.IsAny<StockLevel>()))
                .ReturnsAsync((StockLevel s) => s);
            repos.StockTransactionRepository
                .Setup(s => s.AddAsync(It.IsAny<StockTransaction>()))
                .ReturnsAsync((StockTransaction t) => t);

            return repos;
        }
    }
}

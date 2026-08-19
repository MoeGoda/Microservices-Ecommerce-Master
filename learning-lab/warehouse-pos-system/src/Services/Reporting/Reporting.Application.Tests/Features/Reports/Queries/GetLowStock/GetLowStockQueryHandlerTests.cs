using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Reports.Queries.GetLowStock;
using Reporting.Domain.Entities;
using Xunit;

namespace Reporting.Application.Tests.Features.Reports.Queries.GetLowStock
{
    public class GetLowStockQueryHandlerTests
    {
        private readonly Mock<IStockLevelRecordRepository> _stockLevelRecordRepository = new();

        private GetLowStockQueryHandler CreateHandler() => new(_stockLevelRecordRepository.Object);

        [Fact]
        public async Task Handle_MapsEveryRecordReturnedByRepositoryToADtoWithExactFields()
        {
            var records = new List<StockLevelRecord>
            {
                new()
                {
                    ItemId = 1, Sku = "SKU-1", ItemName = "Widget", LocationId = 3,
                    LocationCode = "WH-3", LocationName = "Main Warehouse",
                    QuantityOnHand = 0, ReorderThreshold = 5,
                    AsOfUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    ItemId = 2, Sku = "SKU-2", ItemName = "Gadget", LocationId = 3,
                    LocationCode = "WH-3", LocationName = "Main Warehouse",
                    QuantityOnHand = 4, ReorderThreshold = 10,
                    AsOfUtc = new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc),
                },
            };
            _stockLevelRecordRepository.Setup(r => r.GetLowStock()).ReturnsAsync(records);
            var handler = CreateHandler();

            var result = (await handler.Handle(new GetLowStockQuery(), CancellationToken.None)).ToList();

            Assert.Equal(2, result.Count);
            var outOfStock = result.Single(d => d.ItemId == 1);
            Assert.Equal(0, outOfStock.QuantityOnHand);
            Assert.Equal(5, outOfStock.ReorderThreshold);
            Assert.Equal("SKU-1", outOfStock.Sku);
            var lowStock = result.Single(d => d.ItemId == 2);
            Assert.Equal(4, lowStock.QuantityOnHand);
            Assert.Equal(10, lowStock.ReorderThreshold);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsNoRows_ReturnsEmptyResult()
        {
            _stockLevelRecordRepository.Setup(r => r.GetLowStock()).ReturnsAsync(Enumerable.Empty<StockLevelRecord>());
            var handler = CreateHandler();

            var result = await handler.Handle(new GetLowStockQuery(), CancellationToken.None);

            Assert.Empty(result);
        }
    }
}

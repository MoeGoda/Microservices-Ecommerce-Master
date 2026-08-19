using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Reports.Queries.GetStockLevels;
using Reporting.Domain.Entities;
using Xunit;

namespace Reporting.Application.Tests.Features.Reports.Queries.GetStockLevels
{
    public class GetStockLevelsQueryHandlerTests
    {
        private readonly Mock<IStockLevelRecordRepository> _stockLevelRecordRepository = new();

        private GetStockLevelsQueryHandler CreateHandler() => new(_stockLevelRecordRepository.Object);

        [Fact]
        public async Task Handle_MapsEveryRecordRegardlessOfThreshold_UnlikeGetLowStock()
        {
            var records = new List<StockLevelRecord>
            {
                new()
                {
                    ItemId = 1, Sku = "SKU-1", ItemName = "Widget", LocationId = 3,
                    LocationCode = "WH-3", LocationName = "Main Warehouse",
                    QuantityOnHand = 500, ReorderThreshold = 5,
                    AsOfUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                },
            };
            _stockLevelRecordRepository.Setup(r => r.GetAll()).ReturnsAsync(records);
            var handler = CreateHandler();

            var result = (await handler.Handle(new GetStockLevelsQuery(), CancellationToken.None)).ToList();

            var dto = Assert.Single(result);
            Assert.Equal(500, dto.QuantityOnHand);
            Assert.Equal("WH-3", dto.LocationCode);
        }
    }
}

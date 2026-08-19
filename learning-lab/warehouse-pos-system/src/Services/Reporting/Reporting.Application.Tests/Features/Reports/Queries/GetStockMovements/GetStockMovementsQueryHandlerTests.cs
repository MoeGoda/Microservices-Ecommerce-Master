using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Reports.Queries.GetStockMovements;
using Reporting.Domain.Entities;
using Xunit;

namespace Reporting.Application.Tests.Features.Reports.Queries.GetStockMovements
{
    public class GetStockMovementsQueryHandlerTests
    {
        private readonly Mock<IStockMovementRecordRepository> _stockMovementRecordRepository = new();

        private GetStockMovementsQueryHandler CreateHandler() => new(_stockMovementRecordRepository.Object);

        [Fact]
        public async Task Handle_PassesAllFourFiltersThroughToTheRepositoryUnchanged()
        {
            var from = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc);
            _stockMovementRecordRepository
                .Setup(r => r.GetPaged(1, 20, from, to, 7, 3))
                .ReturnsAsync((Enumerable.Empty<StockMovementRecord>(), 0));
            var handler = CreateHandler();

            await handler.Handle(
                new GetStockMovementsQuery { Page = 1, PageSize = 20, FromUtc = from, ToUtc = to, ItemId = 7, LocationId = 3 },
                CancellationToken.None);

            _stockMovementRecordRepository.Verify(r => r.GetPaged(1, 20, from, to, 7, 3), Times.Once);
        }

        [Fact]
        public async Task Handle_MapsQuantityChangeAndReferenceExactly()
        {
            var records = new List<StockMovementRecord>
            {
                new()
                {
                    ItemId = 7, Sku = "SKU-7", ItemName = "Widget", LocationId = 3,
                    LocationCode = "WH-3", LocationName = "Main Warehouse",
                    QuantityChange = -5, Reason = "Sale", Reference = "SALE-501",
                    TransactionAtUtc = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    ItemId = 7, Sku = "SKU-7", ItemName = "Widget", LocationId = 3,
                    LocationCode = "WH-3", LocationName = "Main Warehouse",
                    QuantityChange = 20, Reason = "PurchaseOrderReceived", Reference = "PO-12",
                    TransactionAtUtc = new DateTime(2026, 8, 2, 9, 0, 0, DateTimeKind.Utc),
                },
            };
            _stockMovementRecordRepository
                .Setup(r => r.GetPaged(1, 20, null, null, null, null))
                .ReturnsAsync((records, 2));
            var handler = CreateHandler();

            var result = await handler.Handle(new GetStockMovementsQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

            var sale = result.Items.Single(m => m.Reference == "SALE-501");
            Assert.Equal(-5, sale.QuantityChange);
            var receipt = result.Items.Single(m => m.Reference == "PO-12");
            Assert.Equal(20, receipt.QuantityChange);
            Assert.Equal("PurchaseOrderReceived", receipt.Reason);
        }
    }
}

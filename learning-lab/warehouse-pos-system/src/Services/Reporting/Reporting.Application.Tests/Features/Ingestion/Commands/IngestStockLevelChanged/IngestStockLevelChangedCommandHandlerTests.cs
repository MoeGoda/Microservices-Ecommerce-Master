using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Ingestion.Commands.IngestStockLevelChanged;
using Reporting.Domain.Entities;
using Xunit;

namespace Reporting.Application.Tests.Features.Ingestion.Commands.IngestStockLevelChanged
{
    public class IngestStockLevelChangedCommandHandlerTests
    {
        private readonly Mock<IStockLevelRecordRepository> _stockLevelRecordRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private IngestStockLevelChangedCommandHandler CreateHandler() =>
            new(_stockLevelRecordRepository.Object, _unitOfWork.Object);

        private static IngestStockLevelChangedCommand BuildCommand() => new()
        {
            ItemId = 7,
            Sku = "SKU-7",
            ItemName = "Renamed Widget",
            LocationId = 3,
            LocationCode = "WH-3",
            LocationName = "Main Warehouse",
            QuantityOnHand = 42,
            ReorderThreshold = 10,
        };

        [Fact]
        public async Task Handle_NoExistingRecordForPair_InsertsNewRecord()
        {
            _stockLevelRecordRepository.Setup(r => r.GetByItemAndLocation(7, 3)).ReturnsAsync((StockLevelRecord?)null);
            StockLevelRecord? inserted = null;
            _stockLevelRecordRepository
                .Setup(r => r.AddAsync(It.IsAny<StockLevelRecord>()))
                .Callback<StockLevelRecord>(r => inserted = r)
                .ReturnsAsync((StockLevelRecord r) => r);
            var handler = CreateHandler();

            await handler.Handle(BuildCommand(), CancellationToken.None);

            Assert.NotNull(inserted);
            Assert.Equal(42, inserted!.QuantityOnHand);
            Assert.Equal(10, inserted.ReorderThreshold);
            _stockLevelRecordRepository.Verify(r => r.UpdateAsync(It.IsAny<StockLevelRecord>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ExistingRecordForPair_OverwritesQuantityAndRenamedSnapshotFields()
        {
            var existing = new StockLevelRecord
            {
                ItemId = 7,
                Sku = "SKU-7-OLD",
                ItemName = "Old Widget Name",
                LocationId = 3,
                LocationCode = "WH-3",
                LocationName = "Old Warehouse Name",
                QuantityOnHand = 5,
                ReorderThreshold = 10,
                AsOfUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            };
            _stockLevelRecordRepository.Setup(r => r.GetByItemAndLocation(7, 3)).ReturnsAsync(existing);
            var handler = CreateHandler();

            await handler.Handle(BuildCommand(), CancellationToken.None);

            Assert.Equal("Renamed Widget", existing.ItemName);
            Assert.Equal("Main Warehouse", existing.LocationName);
            Assert.Equal(42, existing.QuantityOnHand);
            _stockLevelRecordRepository.Verify(r => r.UpdateAsync(existing), Times.Once);
            _stockLevelRecordRepository.Verify(r => r.AddAsync(It.IsAny<StockLevelRecord>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SameEventDeliveredTwice_SecondDeliveryLeavesQuantityUnchanged()
        {
            var existing = new StockLevelRecord
            {
                ItemId = 7,
                Sku = "SKU-7",
                ItemName = "Renamed Widget",
                LocationId = 3,
                LocationCode = "WH-3",
                LocationName = "Main Warehouse",
                QuantityOnHand = 42,
                ReorderThreshold = 10,
                AsOfUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            };
            _stockLevelRecordRepository.Setup(r => r.GetByItemAndLocation(7, 3)).ReturnsAsync(existing);
            var handler = CreateHandler();

            // Redelivering the identical event is naturally idempotent here —
            // no separate dedup check needed, unlike SaleRecord's ExistsForSale.
            await handler.Handle(BuildCommand(), CancellationToken.None);
            await handler.Handle(BuildCommand(), CancellationToken.None);

            Assert.Equal(42, existing.QuantityOnHand);
            _stockLevelRecordRepository.Verify(r => r.UpdateAsync(existing), Times.Exactly(2));
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        }
    }
}

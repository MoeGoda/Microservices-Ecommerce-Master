using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Ingestion.Commands.IngestStockTransactionRecorded;
using Reporting.Domain.Entities;
using Xunit;

namespace Reporting.Application.Tests.Features.Ingestion.Commands.IngestStockTransactionRecorded
{
    public class IngestStockTransactionRecordedCommandHandlerTests
    {
        private readonly Mock<IStockMovementRecordRepository> _stockMovementRecordRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private IngestStockTransactionRecordedCommandHandler CreateHandler() =>
            new(_stockMovementRecordRepository.Object, _unitOfWork.Object);

        private static IngestStockTransactionRecordedCommand BuildCommand() => new()
        {
            ItemId = 7,
            Sku = "SKU-7",
            ItemName = "Widget",
            LocationId = 3,
            LocationCode = "WH-3",
            LocationName = "Main Warehouse",
            QuantityChange = -5,
            Reason = "Sale",
            Reference = "SALE-501",
            TransactionAtUtc = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
        };

        [Fact]
        public async Task Handle_AnyEvent_InsertsMovementRecordWithExactDelta()
        {
            StockMovementRecord? inserted = null;
            _stockMovementRecordRepository
                .Setup(r => r.AddAsync(It.IsAny<StockMovementRecord>()))
                .Callback<StockMovementRecord>(r => inserted = r)
                .ReturnsAsync((StockMovementRecord r) => r);
            var handler = CreateHandler();

            var result = await handler.Handle(BuildCommand(), CancellationToken.None);

            Assert.False(result.AlreadyProcessed);
            Assert.NotNull(inserted);
            Assert.Equal(-5, inserted!.QuantityChange);
            Assert.Equal("SALE-501", inserted.Reference);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_SameEventDeliveredTwice_InsertsTwoRows()
        {
            // No dedup check exists for this ledger (see StockMovementRecord's
            // own comment) — a repeated delivery double-counts, a named,
            // accepted gap rather than a bug.
            var inserted = new List<StockMovementRecord>();
            _stockMovementRecordRepository
                .Setup(r => r.AddAsync(It.IsAny<StockMovementRecord>()))
                .Callback<StockMovementRecord>(r => inserted.Add(r))
                .ReturnsAsync((StockMovementRecord r) => r);
            var handler = CreateHandler();

            await handler.Handle(BuildCommand(), CancellationToken.None);
            await handler.Handle(BuildCommand(), CancellationToken.None);

            Assert.Equal(2, inserted.Count);
        }
    }
}

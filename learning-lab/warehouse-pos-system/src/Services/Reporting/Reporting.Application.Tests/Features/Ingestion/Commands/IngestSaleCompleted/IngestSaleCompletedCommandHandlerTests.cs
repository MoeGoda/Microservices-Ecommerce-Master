using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Ingestion.Commands.IngestSaleCompleted;
using Reporting.Domain.Entities;
using Xunit;

namespace Reporting.Application.Tests.Features.Ingestion.Commands.IngestSaleCompleted
{
    public class IngestSaleCompletedCommandHandlerTests
    {
        private readonly Mock<ISaleRecordRepository> _saleRecordRepository = new();
        private readonly Mock<ISaleLineRecordRepository> _saleLineRecordRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private IngestSaleCompletedCommandHandler CreateHandler() =>
            new(_saleRecordRepository.Object, _saleLineRecordRepository.Object, _unitOfWork.Object);

        private static Reporting.Application.Features.Ingestion.Commands.IngestSaleCompleted.IngestSaleCompletedCommand BuildCommand() => new()
        {
            SaleId = 501,
            LocationId = 1,
            CashierUserId = 9,
            Total = 47.50m,
            CompletedAtUtc = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
            Lines = new List<IngestSaleCompletedLine>
            {
                new() { ItemId = 1, Sku = "SKU-1", ItemName = "Widget", UnitPrice = 10.00m, Quantity = 2, LineTotal = 20.00m },
                new() { ItemId = 2, Sku = "SKU-2", ItemName = "Gadget", UnitPrice = 27.50m, Quantity = 1, LineTotal = 27.50m },
            },
        };

        [Fact]
        public async Task Handle_NewSale_InsertsSaleRecordWithLineCountMatchingLinesList()
        {
            _saleRecordRepository.Setup(r => r.ExistsForSale(501)).ReturnsAsync(false);
            var command = BuildCommand();
            var handler = CreateHandler();

            SaleRecord? inserted = null;
            _saleRecordRepository
                .Setup(r => r.AddAsync(It.IsAny<SaleRecord>()))
                .Callback<SaleRecord>(r => inserted = r)
                .ReturnsAsync((SaleRecord r) => r);

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.False(result.AlreadyProcessed);
            Assert.NotNull(inserted);
            Assert.Equal(501, inserted!.SaleId);
            Assert.Equal(1, inserted.LocationId);
            Assert.Equal(9, inserted.CashierUserId);
            Assert.Equal(47.50m, inserted.Total);
            Assert.Equal(2, inserted.LineCount);
            Assert.Null(inserted.ReturnedAtUtc);
        }

        [Fact]
        public async Task Handle_NewSale_InsertsOneSaleLineRecordPerLineWithMatchingTotals()
        {
            _saleRecordRepository.Setup(r => r.ExistsForSale(501)).ReturnsAsync(false);
            _saleRecordRepository.Setup(r => r.AddAsync(It.IsAny<SaleRecord>())).ReturnsAsync((SaleRecord r) => r);
            var inserted = new List<SaleLineRecord>();
            _saleLineRecordRepository
                .Setup(r => r.AddAsync(It.IsAny<SaleLineRecord>()))
                .Callback<SaleLineRecord>(l => inserted.Add(l))
                .ReturnsAsync((SaleLineRecord l) => l);
            var handler = CreateHandler();

            await handler.Handle(BuildCommand(), CancellationToken.None);

            Assert.Equal(2, inserted.Count);
            Assert.Equal(20.00m, inserted.Single(l => l.ItemId == 1).LineTotal);
            Assert.Equal(27.50m, inserted.Single(l => l.ItemId == 2).LineTotal);
            Assert.All(inserted, l => Assert.Equal(501, l.SaleId));
        }

        [Fact]
        public async Task Handle_DuplicateSaleId_ReturnsAlreadyProcessedAndInsertsNothing()
        {
            _saleRecordRepository.Setup(r => r.ExistsForSale(501)).ReturnsAsync(true);
            var handler = CreateHandler();

            var result = await handler.Handle(BuildCommand(), CancellationToken.None);

            Assert.True(result.AlreadyProcessed);
            _saleRecordRepository.Verify(r => r.AddAsync(It.IsAny<SaleRecord>()), Times.Never);
            _saleLineRecordRepository.Verify(r => r.AddAsync(It.IsAny<SaleLineRecord>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_NewSale_SavesChangesExactlyOnce()
        {
            _saleRecordRepository.Setup(r => r.ExistsForSale(501)).ReturnsAsync(false);
            _saleRecordRepository.Setup(r => r.AddAsync(It.IsAny<SaleRecord>())).ReturnsAsync((SaleRecord r) => r);
            _saleLineRecordRepository.Setup(r => r.AddAsync(It.IsAny<SaleLineRecord>())).ReturnsAsync((SaleLineRecord l) => l);
            var handler = CreateHandler();

            await handler.Handle(BuildCommand(), CancellationToken.None);

            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}

using Common.Exceptions;
using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Ingestion.Commands.IngestSaleReturned;
using Reporting.Domain.Entities;
using Xunit;

namespace Reporting.Application.Tests.Features.Ingestion.Commands.IngestSaleReturned
{
    public class IngestSaleReturnedCommandHandlerTests
    {
        private readonly Mock<ISaleRecordRepository> _saleRecordRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private IngestSaleReturnedCommandHandler CreateHandler() =>
            new(_saleRecordRepository.Object, _unitOfWork.Object);

        private static SaleRecord BuildUnreturnedRecord() => new()
        {
            SaleId = 501,
            LocationId = 1,
            CashierUserId = 9,
            Total = 47.50m,
            CompletedAtUtc = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
            LineCount = 2,
            ReturnedAtUtc = null,
        };

        [Fact]
        public async Task Handle_UnreturnedSale_SetsReturnedAtUtcAndSavesOnce()
        {
            var record = BuildUnreturnedRecord();
            _saleRecordRepository.Setup(r => r.GetBySaleId(501)).ReturnsAsync(record);
            var handler = CreateHandler();

            var result = await handler.Handle(new IngestSaleReturnedCommand { SaleId = 501 }, CancellationToken.None);

            Assert.False(result.AlreadyProcessed);
            Assert.NotNull(record.ReturnedAtUtc);
            _saleRecordRepository.Verify(r => r.UpdateAsync(record), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_AlreadyReturnedSale_ReturnsAlreadyProcessedAndLeavesReturnedAtUtcUnchanged()
        {
            var originalReturnedAt = new DateTime(2026, 8, 2, 8, 0, 0, DateTimeKind.Utc);
            var record = BuildUnreturnedRecord();
            record.ReturnedAtUtc = originalReturnedAt;
            _saleRecordRepository.Setup(r => r.GetBySaleId(501)).ReturnsAsync(record);
            var handler = CreateHandler();

            var result = await handler.Handle(new IngestSaleReturnedCommand { SaleId = 501 }, CancellationToken.None);

            Assert.True(result.AlreadyProcessed);
            Assert.Equal(originalReturnedAt, record.ReturnedAtUtc);
            _saleRecordRepository.Verify(r => r.UpdateAsync(It.IsAny<SaleRecord>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_SaleNotYetIngested_ThrowsNotFoundException()
        {
            _saleRecordRepository.Setup(r => r.GetBySaleId(999)).ReturnsAsync((SaleRecord?)null);
            var handler = CreateHandler();

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(new IngestSaleReturnedCommand { SaleId = 999 }, CancellationToken.None));

            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }
}

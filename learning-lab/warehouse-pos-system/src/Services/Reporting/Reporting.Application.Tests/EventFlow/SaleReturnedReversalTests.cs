using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Ingestion.Commands.IngestSaleCompleted;
using Reporting.Application.Features.Ingestion.Commands.IngestSaleReturned;
using Reporting.Application.Features.Reports.Queries.GetCashierPerformance;
using Reporting.Application.Features.Reports.Queries.GetSalesByDay;
using Reporting.Application.Tests.Testing;
using Xunit;

namespace Reporting.Application.Tests.EventFlow
{
    // Exercises IngestSaleCompletedCommandHandler and
    // IngestSaleReturnedCommandHandler back to back against a
    // FakeSaleRecordRepository (see Testing/FakeSaleRecordRepository) that
    // actually reproduces the read model's GROUP BY/filter behavior, then
    // reads the result back through the real query handlers — a plain Moq
    // mock can't prove a SaleReturned event actually changes what a later
    // report sees, only that the handler called some method.
    public class SaleReturnedReversalTests
    {
        private readonly FakeSaleRecordRepository _saleRecordRepository = new();
        private readonly Mock<ISaleLineRecordRepository> _saleLineRecordRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private IngestSaleCompletedCommandHandler CreateIngestHandler() =>
            new(_saleRecordRepository, _saleLineRecordRepository.Object, _unitOfWork.Object);

        private IngestSaleReturnedCommandHandler CreateReturnHandler() =>
            new(_saleRecordRepository, _unitOfWork.Object);

        private static IngestSaleCompletedCommand BuildSale(int saleId, decimal total, DateTime completedAtUtc, int cashierUserId = 9) => new()
        {
            SaleId = saleId,
            LocationId = 1,
            CashierUserId = cashierUserId,
            Total = total,
            CompletedAtUtc = completedAtUtc,
            Lines = new List<IngestSaleCompletedLine>
            {
                new() { ItemId = 1, Sku = "SKU-1", ItemName = "Widget", UnitPrice = total, Quantity = 1, LineTotal = total },
            },
        };

        [Fact]
        public async Task ThreeSalesOnOneDay_GetSalesByDay_ReturnsExactlyThatDayAndTotal()
        {
            var day = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
            var ingest = CreateIngestHandler();
            await ingest.Handle(BuildSale(1, 10.00m, day.AddHours(1)), CancellationToken.None);
            await ingest.Handle(BuildSale(2, 12.50m, day.AddHours(2)), CancellationToken.None);
            await ingest.Handle(BuildSale(3, 25.00m, day.AddHours(3)), CancellationToken.None);

            var report = await new GetSalesByDayQueryHandler(_saleRecordRepository)
                .Handle(new GetSalesByDayQuery(), CancellationToken.None);

            var onlyDay = Assert.Single(report);
            Assert.Equal(new DateOnly(2026, 8, 1), onlyDay.Date);
            Assert.Equal(3, onlyDay.SaleCount);
            Assert.Equal(47.50m, onlyDay.Total);
        }

        [Fact]
        public async Task SaleReturned_RemovesTheSaleFromSalesByDayTotalsEntirely()
        {
            var day = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
            var ingest = CreateIngestHandler();
            await ingest.Handle(BuildSale(1, 10.00m, day.AddHours(1)), CancellationToken.None);
            await ingest.Handle(BuildSale(2, 12.50m, day.AddHours(2)), CancellationToken.None);

            await CreateReturnHandler().Handle(new IngestSaleReturnedCommand { SaleId = 1 }, CancellationToken.None);

            var report = await new GetSalesByDayQueryHandler(_saleRecordRepository)
                .Handle(new GetSalesByDayQuery(), CancellationToken.None);

            var onlyDay = Assert.Single(report);
            Assert.Equal(1, onlyDay.SaleCount);
            Assert.Equal(12.50m, onlyDay.Total);
        }

        [Fact]
        public async Task SaleReturned_ThenSalesByDayHasNoRowsLeftForThatDay_WhenItWasTheOnlySale()
        {
            var day = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
            await CreateIngestHandler().Handle(BuildSale(1, 10.00m, day), CancellationToken.None);
            await CreateReturnHandler().Handle(new IngestSaleReturnedCommand { SaleId = 1 }, CancellationToken.None);

            var report = await new GetSalesByDayQueryHandler(_saleRecordRepository)
                .Handle(new GetSalesByDayQuery(), CancellationToken.None);

            Assert.Empty(report);
        }

        [Fact]
        public async Task SaleReturned_StillCountsTowardCashierPerformanceAsAReturnButNotAsRevenue()
        {
            var day = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
            var ingest = CreateIngestHandler();
            await ingest.Handle(BuildSale(1, 10.00m, day.AddHours(1)), CancellationToken.None);
            await ingest.Handle(BuildSale(2, 20.00m, day.AddHours(2)), CancellationToken.None);
            await CreateReturnHandler().Handle(new IngestSaleReturnedCommand { SaleId = 1 }, CancellationToken.None);

            var performance = (await new GetCashierPerformanceQueryHandler(_saleRecordRepository)
                .Handle(new GetCashierPerformanceQuery(), CancellationToken.None)).Single();

            Assert.Equal(1, performance.CompletedSaleCount);
            Assert.Equal(1, performance.ReturnedSaleCount);
            Assert.Equal(20.00m, performance.TotalRevenue);
            Assert.Equal(20.00m, performance.AverageSaleTotal);
        }

        [Fact]
        public async Task DuplicateSaleCompletedDelivery_DoesNotDoubleCountTheDaysTotal()
        {
            var day = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
            var ingest = CreateIngestHandler();
            var command = BuildSale(1, 10.00m, day);

            await ingest.Handle(command, CancellationToken.None);
            var secondDelivery = await ingest.Handle(command, CancellationToken.None);

            var report = await new GetSalesByDayQueryHandler(_saleRecordRepository)
                .Handle(new GetSalesByDayQuery(), CancellationToken.None);

            Assert.True(secondDelivery.AlreadyProcessed);
            var onlyDay = Assert.Single(report);
            Assert.Equal(1, onlyDay.SaleCount);
            Assert.Equal(10.00m, onlyDay.Total);
        }

        [Fact]
        public async Task SaleReturnedArrivingBeforeSaleCompleted_ThrowsRatherThanSilentlyCreatingAPhantomRecord()
        {
            // Out-of-order delivery: the two events for the same sale can
            // arrive from POS's outbox in either order. Confirms the
            // handler surfaces this as a retryable failure instead of
            // creating a SaleRecord out of thin air.
            await Assert.ThrowsAsync<Common.Exceptions.NotFoundException>(
                () => CreateReturnHandler().Handle(new IngestSaleReturnedCommand { SaleId = 404 }, CancellationToken.None));
        }
    }
}

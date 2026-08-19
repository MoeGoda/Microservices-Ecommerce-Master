using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Reports.Queries.GetCashierPerformance;
using Reporting.Application.Models;
using Xunit;

namespace Reporting.Application.Tests.Features.Reports.Queries.GetCashierPerformance
{
    // The per-cashier GROUP BY (and the AverageSaleTotal division) lives in
    // ISaleRecordRepository.GetCashierPerformance's EF Core implementation;
    // this only proves the date range is forwarded and the result comes
    // back unmodified.
    public class GetCashierPerformanceQueryHandlerTests
    {
        private readonly Mock<ISaleRecordRepository> _saleRecordRepository = new();

        private GetCashierPerformanceQueryHandler CreateHandler() => new(_saleRecordRepository.Object);

        [Fact]
        public async Task Handle_ForwardsTheDateRangeAndReturnsTheRepositoryResultUnmodified()
        {
            var from = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc);
            var performance = new List<CashierPerformanceDto>
            {
                new() { CashierUserId = 9, CompletedSaleCount = 3, ReturnedSaleCount = 1, TotalRevenue = 90.00m, AverageSaleTotal = 30.00m },
            };
            _saleRecordRepository.Setup(r => r.GetCashierPerformance(from, to)).ReturnsAsync(performance);
            var handler = CreateHandler();

            var result = (await handler.Handle(new GetCashierPerformanceQuery { FromUtc = from, ToUtc = to }, CancellationToken.None)).ToList();

            _saleRecordRepository.Verify(r => r.GetCashierPerformance(from, to), Times.Once);
            Assert.Equal(30.00m, result.Single().AverageSaleTotal);
        }
    }
}

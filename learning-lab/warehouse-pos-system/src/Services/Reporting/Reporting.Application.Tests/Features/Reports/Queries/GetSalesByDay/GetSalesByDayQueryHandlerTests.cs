using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Reports.Queries.GetSalesByDay;
using Reporting.Application.Models;
using Xunit;

namespace Reporting.Application.Tests.Features.Reports.Queries.GetSalesByDay
{
    // GetSalesByDayQueryHandler has no logic of its own — the real GROUP BY
    // (and the exclude-returned-sales filter) lives in
    // ISaleRecordRepository.GetSalesByDay's EF Core implementation, which
    // this Application-layer test suite mocks away rather than exercises.
    // These tests only prove the handler is wired to the right repository
    // method and returns its result untouched.
    public class GetSalesByDayQueryHandlerTests
    {
        private readonly Mock<ISaleRecordRepository> _saleRecordRepository = new();

        private GetSalesByDayQueryHandler CreateHandler() => new(_saleRecordRepository.Object);

        [Fact]
        public async Task Handle_ReturnsExactlyWhatTheRepositoryAggregated()
        {
            var aggregate = new List<SalesByDayDto>
            {
                new() { Date = new DateOnly(2026, 8, 1), SaleCount = 3, Total = 47.50m },
                new() { Date = new DateOnly(2026, 8, 2), SaleCount = 1, Total = 15.00m },
            };
            _saleRecordRepository.Setup(r => r.GetSalesByDay()).ReturnsAsync(aggregate);
            var handler = CreateHandler();

            var result = (await handler.Handle(new GetSalesByDayQuery(), CancellationToken.None)).ToList();

            Assert.Equal(2, result.Count);
            var day = result.Single(d => d.Date == new DateOnly(2026, 8, 1));
            Assert.Equal(3, day.SaleCount);
            Assert.Equal(47.50m, day.Total);
        }
    }
}

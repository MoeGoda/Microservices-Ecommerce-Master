using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Reports.Queries.GetSalesLedger;
using Reporting.Domain.Entities;
using Xunit;

namespace Reporting.Application.Tests.Features.Reports.Queries.GetSalesLedger
{
    public class GetSalesLedgerQueryHandlerTests
    {
        private readonly Mock<ISaleRecordRepository> _saleRecordRepository = new();

        private GetSalesLedgerQueryHandler CreateHandler() => new(_saleRecordRepository.Object);

        [Fact]
        public async Task Handle_PassesTheRequestedDateRangeStraightToTheRepository()
        {
            var from = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc);
            _saleRecordRepository
                .Setup(r => r.GetLedgerPaged(1, 20, from, to))
                .ReturnsAsync((Enumerable.Empty<SaleRecord>(), 0));
            var handler = CreateHandler();

            await handler.Handle(new GetSalesLedgerQuery { Page = 1, PageSize = 20, FromUtc = from, ToUtc = to }, CancellationToken.None);

            _saleRecordRepository.Verify(r => r.GetLedgerPaged(1, 20, from, to), Times.Once);
        }

        [Fact]
        public async Task Handle_UnlikeSalesByDay_KeepsAReturnedSaleInTheResultWithItsReturnedAtUtcSet()
        {
            var returnedAt = new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);
            var records = new List<SaleRecord>
            {
                new()
                {
                    SaleId = 501, LocationId = 1, CashierUserId = 9, Total = 47.50m,
                    CompletedAtUtc = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
                    LineCount = 2, ReturnedAtUtc = returnedAt,
                },
                new()
                {
                    SaleId = 502, LocationId = 1, CashierUserId = 9, Total = 15.00m,
                    CompletedAtUtc = new DateTime(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc),
                    LineCount = 1, ReturnedAtUtc = null,
                },
            };
            _saleRecordRepository
                .Setup(r => r.GetLedgerPaged(1, 20, null, null))
                .ReturnsAsync((records, 2));
            var handler = CreateHandler();

            var result = await handler.Handle(new GetSalesLedgerQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

            var returnedEntry = result.Items.Single(e => e.SaleId == 501);
            Assert.Equal(returnedAt, returnedEntry.ReturnedAtUtc);
            var activeEntry = result.Items.Single(e => e.SaleId == 502);
            Assert.Null(activeEntry.ReturnedAtUtc);
        }
    }
}

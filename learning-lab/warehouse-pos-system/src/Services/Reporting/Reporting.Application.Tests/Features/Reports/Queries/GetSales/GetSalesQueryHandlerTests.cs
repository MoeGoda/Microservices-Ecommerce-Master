using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Reports.Queries.GetSales;
using Reporting.Domain.Entities;
using Xunit;

namespace Reporting.Application.Tests.Features.Reports.Queries.GetSales
{
    public class GetSalesQueryHandlerTests
    {
        private readonly Mock<ISaleRecordRepository> _saleRecordRepository = new();

        private GetSalesQueryHandler CreateHandler() => new(_saleRecordRepository.Object);

        [Fact]
        public async Task Handle_MapsEachRecordAndComputesTotalPagesFromTotalCount()
        {
            var records = new List<SaleRecord>
            {
                new() { SaleId = 1, LocationId = 1, CashierUserId = 9, Total = 10m, CompletedAtUtc = DateTime.UtcNow, LineCount = 1 },
                new() { SaleId = 2, LocationId = 1, CashierUserId = 9, Total = 20m, CompletedAtUtc = DateTime.UtcNow, LineCount = 1 },
            };
            // 47 total rows at a page size of 20 spans 3 pages (ceil(47/20) = 3),
            // not 2 — this is the boundary GetSalesQueryHandler's PagedResult
            // math has to get right.
            _saleRecordRepository.Setup(r => r.GetPaged(2, 20)).ReturnsAsync((records, 47));
            var handler = CreateHandler();

            var result = await handler.Handle(new GetSalesQuery { Page = 2, PageSize = 20 }, CancellationToken.None);

            Assert.Equal(2, result.Items.Count);
            Assert.Equal(1, result.Items[0].SaleId);
            Assert.Equal(10m, result.Items[0].Total);
            Assert.Equal(47, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
            Assert.Equal(2, result.Page);
            Assert.Equal(20, result.PageSize);
        }

        [Fact]
        public async Task Handle_TotalCountExactMultipleOfPageSize_DoesNotAddAnExtraPage()
        {
            _saleRecordRepository.Setup(r => r.GetPaged(1, 20)).ReturnsAsync((Enumerable.Empty<SaleRecord>(), 40));
            var handler = CreateHandler();

            var result = await handler.Handle(new GetSalesQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

            Assert.Equal(2, result.TotalPages);
        }
    }
}

using Moq;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Features.Reports.Queries.GetTopSellingItems;
using Reporting.Application.Models;
using Xunit;

namespace Reporting.Application.Tests.Features.Reports.Queries.GetTopSellingItems
{
    // The revenue-ranked GROUP BY itself lives in
    // ISaleLineRecordRepository.GetTopSellingItems's EF Core implementation;
    // this only proves Take is forwarded and the ranked result comes back
    // unmodified.
    public class GetTopSellingItemsQueryHandlerTests
    {
        private readonly Mock<ISaleLineRecordRepository> _saleLineRecordRepository = new();

        private GetTopSellingItemsQueryHandler CreateHandler() => new(_saleLineRecordRepository.Object);

        [Fact]
        public async Task Handle_ForwardsTakeToTheRepositoryAndReturnsItsResultInOrder()
        {
            var ranked = new List<TopSellingItemDto>
            {
                new() { ItemId = 1, Sku = "SKU-1", ItemName = "Widget", TotalQuantity = 1, TotalRevenue = 100.00m },
                new() { ItemId = 2, Sku = "SKU-2", ItemName = "Gadget", TotalQuantity = 5, TotalRevenue = 20.00m },
            };
            _saleLineRecordRepository.Setup(r => r.GetTopSellingItems(5)).ReturnsAsync(ranked);
            var handler = CreateHandler();

            var result = (await handler.Handle(new GetTopSellingItemsQuery { Take = 5 }, CancellationToken.None)).ToList();

            _saleLineRecordRepository.Verify(r => r.GetTopSellingItems(5), Times.Once);
            Assert.Equal(2, result.Count);
            Assert.Equal(100.00m, result[0].TotalRevenue);
        }
    }
}

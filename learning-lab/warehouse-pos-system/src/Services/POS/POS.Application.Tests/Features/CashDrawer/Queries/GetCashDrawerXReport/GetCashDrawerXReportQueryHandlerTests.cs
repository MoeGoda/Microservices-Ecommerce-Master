using Common.Exceptions;
using Moq;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.CashDrawer.Queries.GetCashDrawerXReport;
using POS.Domain.Entities;
using Xunit;

namespace POS.Application.Tests.Features.CashDrawer.Queries
{
    public class GetCashDrawerXReportQueryHandlerTests
    {
        private readonly Mock<ICashDrawerRepository> _cashDrawerRepository = new();
        private readonly Mock<ISaleRepository> _saleRepository = new();

        private GetCashDrawerXReportQueryHandler CreateHandler() => new(
            _cashDrawerRepository.Object,
            _saleRepository.Object);

        [Fact]
        public async Task Handle_SessionNotFound_ThrowsNotFoundException()
        {
            _cashDrawerRepository.Setup(r => r.GetSessionById(1)).ReturnsAsync((CashDrawerSession?)null);
            var handler = CreateHandler();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new GetCashDrawerXReportQuery { SessionId = 1 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ExpectedCashInDrawer_ExcludesSalesTotal()
        {
            var openedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
            var session = new CashDrawerSession { Id = 5, LocationId = 1, OpeningFloat = 100m, OpenedAt = openedAt };
            _cashDrawerRepository.Setup(r => r.GetSessionById(5)).ReturnsAsync(session);
            _cashDrawerRepository.Setup(r => r.GetMovements(5)).ReturnsAsync(new[]
            {
                new CashMovement { Type = CashMovementType.CashIn, Amount = 50m },
                new CashMovement { Type = CashMovementType.CashOut, Amount = 30m },
            });
            // Even though completed sales exist, there's no split-tender/payment-
            // method field to know how much of a sale was paid in cash, so
            // SalesTotal is deliberately excluded from ExpectedCashInDrawer.
            _saleRepository
                .Setup(r => r.GetCompletedSince(1, openedAt))
                .ReturnsAsync(new[] { new Sale { Id = 1, Total = 200m }, new Sale { Id = 2, Total = 75m } });

            var handler = CreateHandler();
            var result = await handler.Handle(new GetCashDrawerXReportQuery { SessionId = 5 }, CancellationToken.None);

            Assert.Equal(100m, result.OpeningFloat);
            Assert.Equal(50m, result.CashInTotal);
            Assert.Equal(30m, result.CashOutTotal);
            Assert.Equal(2, result.CompletedSaleCount);
            Assert.Equal(275m, result.SalesTotal);
            Assert.Equal(120m, result.ExpectedCashInDrawer);
        }
    }
}

using Common.Exceptions;
using Microsoft.Extensions.Options;
using Moq;
using POS.Application.Common;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.Sales.Commands.SetLineDiscount;
using POS.Domain.Entities;
using Xunit;

namespace POS.Application.Tests.Features.Sales.Commands
{
    public class SetLineDiscountCommandHandlerTests
    {
        private readonly Mock<ISaleRepository> _saleRepository = new();
        private readonly Mock<ISaleLineRepository> _saleLineRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly IOptions<TaxSettings> _taxSettings = Options.Create(new TaxSettings { RatePercent = 10m });

        private SetLineDiscountCommandHandler CreateHandler() => new(
            _saleRepository.Object,
            _saleLineRepository.Object,
            _unitOfWork.Object,
            _taxSettings);

        [Fact]
        public async Task Handle_LineAlreadyHasPromotion_ThrowsConflictException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(new Sale { Id = 1, Status = SaleStatus.InProgress });
            _saleLineRepository.Setup(r => r.GetById(1)).ReturnsAsync(new SaleLine { Id = 1, SaleId = 1, PromotionId = 42 });
            var handler = CreateHandler();

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(new SetLineDiscountCommand { SaleId = 1, SaleLineId = 1, ManualDiscountPercent = 10m }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ClearingDiscountOnPromotionLine_IsAllowed()
        {
            var sale = new Sale { Id = 1, Status = SaleStatus.InProgress };
            var line = new SaleLine { Id = 1, SaleId = 1, PromotionId = 42, UnitPrice = 8m, Quantity = 1 };
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            _saleLineRepository.Setup(r => r.GetById(1)).ReturnsAsync(line);
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(new[] { line });

            var handler = CreateHandler();
            await handler.Handle(new SetLineDiscountCommand { SaleId = 1, SaleLineId = 1, ManualDiscountPercent = null }, CancellationToken.None);

            Assert.Null(line.ManualDiscountPercent);
            Assert.Equal(8m, line.LineTotal);
        }

        [Fact]
        public async Task Handle_SetsDiscountAndRecomputesLineAndSaleTotals()
        {
            var sale = new Sale { Id = 1, Status = SaleStatus.InProgress };
            var line = new SaleLine { Id = 1, SaleId = 1, UnitPrice = 10m, Quantity = 2 };
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            _saleLineRepository.Setup(r => r.GetById(1)).ReturnsAsync(line);
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(new[] { line });

            var handler = CreateHandler();
            await handler.Handle(new SetLineDiscountCommand { SaleId = 1, SaleLineId = 1, ManualDiscountPercent = 25m }, CancellationToken.None);

            // 10 x 2 x (1 - 0.25) = 15
            Assert.Equal(15m, line.LineTotal);
            Assert.Equal(15m, sale.NetTotal);
            Assert.Equal(1.5m, sale.TaxAmount);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}

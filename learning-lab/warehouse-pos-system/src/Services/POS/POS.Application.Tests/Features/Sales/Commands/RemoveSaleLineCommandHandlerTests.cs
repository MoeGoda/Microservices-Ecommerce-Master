using Common.Exceptions;
using Microsoft.Extensions.Options;
using Moq;
using POS.Application.Common;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.Sales.Commands.RemoveSaleLine;
using POS.Domain.Entities;
using Xunit;

namespace POS.Application.Tests.Features.Sales.Commands
{
    public class RemoveSaleLineCommandHandlerTests
    {
        private readonly Mock<ISaleRepository> _saleRepository = new();
        private readonly Mock<ISaleLineRepository> _saleLineRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly IOptions<TaxSettings> _taxSettings = Options.Create(new TaxSettings { RatePercent = 10m });

        private RemoveSaleLineCommandHandler CreateHandler() => new(
            _saleRepository.Object,
            _saleLineRepository.Object,
            _unitOfWork.Object,
            _taxSettings);

        [Fact]
        public async Task Handle_SaleNotFound_ThrowsNotFoundException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync((Sale?)null);
            var handler = CreateHandler();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new RemoveSaleLineCommand { SaleId = 1, SaleLineId = 1 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_SaleNotInProgress_ThrowsConflictException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(new Sale { Id = 1, Status = SaleStatus.Completed });
            var handler = CreateHandler();

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(new RemoveSaleLineCommand { SaleId = 1, SaleLineId = 1 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_LineBelongsToDifferentSale_ThrowsNotFoundException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(new Sale { Id = 1, Status = SaleStatus.InProgress });
            _saleLineRepository.Setup(r => r.GetById(99)).ReturnsAsync(new SaleLine { Id = 99, SaleId = 2 });
            var handler = CreateHandler();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new RemoveSaleLineCommand { SaleId = 1, SaleLineId = 99 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_RemovesLineAndRecomputesRemainingTotals()
        {
            var sale = new Sale { Id = 1, Status = SaleStatus.InProgress };
            var toRemove = new SaleLine { Id = 1, SaleId = 1, LineTotal = 10m };
            var remaining = new SaleLine { Id = 2, SaleId = 1, LineTotal = 20m };

            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            _saleLineRepository.Setup(r => r.GetById(1)).ReturnsAsync(toRemove);
            // Simulates the repository still returning the just-deleted row
            // in the same unit of work — the handler's own .Where(...) filter
            // is what has to exclude it, not the repository.
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(new[] { toRemove, remaining });

            var handler = CreateHandler();
            await handler.Handle(new RemoveSaleLineCommand { SaleId = 1, SaleLineId = 1 }, CancellationToken.None);

            _saleLineRepository.Verify(r => r.DeleteAsync(toRemove), Times.Once);
            Assert.Equal(20m, sale.NetTotal);
            Assert.Equal(2m, sale.TaxAmount);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}

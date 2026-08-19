using Common.Exceptions;
using Moq;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.Sales.Commands.SetSaleCustomer;
using POS.Domain.Entities;
using Xunit;

namespace POS.Application.Tests.Features.Sales.Commands
{
    public class SetSaleCustomerCommandHandlerTests
    {
        private readonly Mock<ISaleRepository> _saleRepository = new();
        private readonly Mock<ISaleLineRepository> _saleLineRepository = new();
        private readonly Mock<ICustomerRepository> _customerRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private SetSaleCustomerCommandHandler CreateHandler() => new(
            _saleRepository.Object,
            _saleLineRepository.Object,
            _customerRepository.Object,
            _unitOfWork.Object);

        [Fact]
        public async Task Handle_CustomerIdDoesNotExist_ThrowsNotFoundException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(new Sale { Id = 1, Status = SaleStatus.InProgress });
            _customerRepository.Setup(r => r.GetById(99)).ReturnsAsync((Customer?)null);
            var handler = CreateHandler();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new SetSaleCustomerCommand { SaleId = 1, CustomerId = 99 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ValidCustomer_AttachesCustomerToSale()
        {
            var sale = new Sale { Id = 1, Status = SaleStatus.InProgress };
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            _customerRepository.Setup(r => r.GetById(7)).ReturnsAsync(new Customer { Id = 7, Name = "Jane" });
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(Array.Empty<SaleLine>());

            var handler = CreateHandler();
            await handler.Handle(new SetSaleCustomerCommand { SaleId = 1, CustomerId = 7 }, CancellationToken.None);

            Assert.Equal(7, sale.CustomerId);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_NullCustomerId_DetachesCustomerWithoutLookup()
        {
            var sale = new Sale { Id = 1, Status = SaleStatus.InProgress, CustomerId = 7 };
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(Array.Empty<SaleLine>());

            var handler = CreateHandler();
            await handler.Handle(new SetSaleCustomerCommand { SaleId = 1, CustomerId = null }, CancellationToken.None);

            Assert.Null(sale.CustomerId);
            _customerRepository.Verify(r => r.GetById(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SaleNotInProgress_ThrowsConflictException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(new Sale { Id = 1, Status = SaleStatus.Completed });
            var handler = CreateHandler();

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(new SetSaleCustomerCommand { SaleId = 1, CustomerId = 7 }, CancellationToken.None));
        }
    }
}

using Common.Exceptions;
using Moq;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.Customers.Commands.AdjustCustomerBalance;
using POS.Domain.Entities;
using Xunit;

namespace POS.Application.Tests.Features.Customers.Commands
{
    public class AdjustCustomerBalanceCommandHandlerTests
    {
        private readonly Mock<ICustomerRepository> _customerRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private AdjustCustomerBalanceCommandHandler CreateHandler() => new(
            _customerRepository.Object,
            _unitOfWork.Object);

        [Fact]
        public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
        {
            _customerRepository.Setup(r => r.GetById(1)).ReturnsAsync((Customer?)null);
            var handler = CreateHandler();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new AdjustCustomerBalanceCommand { CustomerId = 1, Delta = 10m }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_PositiveDelta_IncreasesBalance()
        {
            var customer = new Customer { Id = 1, Name = "Jane", Balance = 20m };
            _customerRepository.Setup(r => r.GetById(1)).ReturnsAsync(customer);
            var handler = CreateHandler();

            await handler.Handle(new AdjustCustomerBalanceCommand { CustomerId = 1, Delta = 15m }, CancellationToken.None);

            Assert.Equal(35m, customer.Balance);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_NegativeDelta_DecreasesBalance()
        {
            var customer = new Customer { Id = 1, Name = "Jane", Balance = 20m };
            _customerRepository.Setup(r => r.GetById(1)).ReturnsAsync(customer);
            var handler = CreateHandler();

            await handler.Handle(new AdjustCustomerBalanceCommand { CustomerId = 1, Delta = -5m }, CancellationToken.None);

            Assert.Equal(15m, customer.Balance);
        }
    }
}

using Moq;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.Customers.Commands.CreateCustomer;
using POS.Domain.Entities;
using Xunit;

namespace POS.Application.Tests.Features.Customers.Commands
{
    public class CreateCustomerCommandHandlerTests
    {
        private readonly Mock<ICustomerRepository> _customerRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private CreateCustomerCommandHandler CreateHandler() => new(
            _customerRepository.Object,
            _unitOfWork.Object);

        [Fact]
        public async Task Handle_ValidRequest_CreatesCustomerWithZeroedDefaults()
        {
            Customer? added = null;
            _customerRepository
                .Setup(r => r.AddAsync(It.IsAny<Customer>()))
                .Callback<Customer>(c => added = c)
                .ReturnsAsync((Customer c) => c);

            var handler = CreateHandler();
            var result = await handler.Handle(
                new CreateCustomerCommand { Name = "Jane", Phone = "555-1234", Email = "jane@example.com" },
                CancellationToken.None);

            Assert.NotNull(added);
            Assert.Equal("Jane", added!.Name);
            Assert.Equal("555-1234", added.Phone);
            Assert.Equal("jane@example.com", added.Email);
            Assert.Equal(0, added.LoyaltyPoints);
            Assert.Equal(0m, added.Balance);
            Assert.Equal("Jane", result.Name);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}

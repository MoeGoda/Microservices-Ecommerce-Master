using Common.Exceptions;
using Moq;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.Outbox;
using POS.Application.Features.Sales.Commands.Checkout;
using POS.Domain.Entities;
using Xunit;

namespace POS.Application.Tests.Features.Sales.Commands
{
    public class CheckoutCommandHandlerTests
    {
        private readonly Mock<ISaleRepository> _saleRepository = new();
        private readonly Mock<ISaleLineRepository> _saleLineRepository = new();
        private readonly Mock<ICustomerRepository> _customerRepository = new();
        private readonly Mock<IOutboxRepository> _outboxRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private CheckoutCommandHandler CreateHandler() => new(
            _saleRepository.Object,
            _saleLineRepository.Object,
            _customerRepository.Object,
            _outboxRepository.Object,
            _unitOfWork.Object);

        [Fact]
        public async Task Handle_SaleNotFound_ThrowsNotFoundException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync((Sale?)null);
            var handler = CreateHandler();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new CheckoutCommand { SaleId = 1 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_SaleNotInProgress_ThrowsConflictException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(new Sale { Id = 1, Status = SaleStatus.Completed });
            var handler = CreateHandler();

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(new CheckoutCommand { SaleId = 1 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NoLines_ThrowsConflictException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(new Sale { Id = 1, Status = SaleStatus.InProgress });
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(Array.Empty<SaleLine>());
            var handler = CreateHandler();

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(new CheckoutCommand { SaleId = 1 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NoCustomerAttached_CompletesWithoutTouchingCustomerRepository()
        {
            var sale = new Sale { Id = 1, Status = SaleStatus.InProgress, Total = 39.06m, CustomerId = null };
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(new[] { new SaleLine { Id = 1, SaleId = 1, LineTotal = 39.06m } });
            SetUpOutboxCapture();

            var handler = CreateHandler();
            var result = await handler.Handle(new CheckoutCommand { SaleId = 1 }, CancellationToken.None);

            Assert.Equal("Completed", result.Status);
            Assert.NotNull(sale.CompletedAt);
            Assert.Equal(StockSyncStatus.Pending, sale.StockSyncStatus);
            _customerRepository.Verify(r => r.GetById(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CustomerAttached_EarnsOnePointPerTenDollarsFloored()
        {
            var sale = new Sale { Id = 1, Status = SaleStatus.InProgress, Total = 39.06m, CustomerId = 7 };
            var customer = new Customer { Id = 7, Name = "Jane", LoyaltyPoints = 5 };
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(new[] { new SaleLine { Id = 1, SaleId = 1, LineTotal = 39.06m } });
            _customerRepository.Setup(r => r.GetById(7)).ReturnsAsync(customer);
            SetUpOutboxCapture();

            var handler = CreateHandler();
            await handler.Handle(new CheckoutCommand { SaleId = 1 }, CancellationToken.None);

            // floor(39.06 / 10) = 3, added to the existing 5.
            Assert.Equal(8, customer.LoyaltyPoints);
            _customerRepository.Verify(r => r.UpdateAsync(customer), Times.Once);
        }

        [Fact]
        public async Task Handle_TotalUnderTenDollars_EarnsZeroPoints()
        {
            var sale = new Sale { Id = 1, Status = SaleStatus.InProgress, Total = 6.87m, CustomerId = 7 };
            var customer = new Customer { Id = 7, Name = "Jane", LoyaltyPoints = 0 };
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(new[] { new SaleLine { Id = 1, SaleId = 1, LineTotal = 6.87m } });
            _customerRepository.Setup(r => r.GetById(7)).ReturnsAsync(customer);
            SetUpOutboxCapture();

            var handler = CreateHandler();
            await handler.Handle(new CheckoutCommand { SaleId = 1 }, CancellationToken.None);

            Assert.Equal(0, customer.LoyaltyPoints);
        }

        [Fact]
        public async Task Handle_QueuesOneOutboxMessageWithADeliveryPerConsumer()
        {
            var sale = new Sale { Id = 1, Status = SaleStatus.InProgress, Total = 10m };
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(new[] { new SaleLine { Id = 1, SaleId = 1, LineTotal = 10m } });

            var deliveries = new List<OutboxDelivery>();
            _outboxRepository
                .Setup(r => r.AddMessageAsync(It.Is<OutboxMessage>(m => m.EventType == OutboxEventTypes.SaleCompleted)))
                .ReturnsAsync((OutboxMessage m) => m);
            _outboxRepository
                .Setup(r => r.AddDeliveryAsync(It.IsAny<OutboxDelivery>()))
                .Callback<OutboxDelivery>(deliveries.Add)
                .ReturnsAsync((OutboxDelivery d) => d);

            var handler = CreateHandler();
            await handler.Handle(new CheckoutCommand { SaleId = 1 }, CancellationToken.None);

            Assert.Equal(3, deliveries.Count);
            Assert.Contains(deliveries, d => d.ConsumerName == OutboxConsumers.Warehouse);
            Assert.Contains(deliveries, d => d.ConsumerName == OutboxConsumers.Reporting);
            Assert.Contains(deliveries, d => d.ConsumerName == OutboxConsumers.Notifications);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        private void SetUpOutboxCapture()
        {
            _outboxRepository.Setup(r => r.AddMessageAsync(It.IsAny<OutboxMessage>())).ReturnsAsync((OutboxMessage m) => m);
            _outboxRepository.Setup(r => r.AddDeliveryAsync(It.IsAny<OutboxDelivery>())).ReturnsAsync((OutboxDelivery d) => d);
        }
    }
}

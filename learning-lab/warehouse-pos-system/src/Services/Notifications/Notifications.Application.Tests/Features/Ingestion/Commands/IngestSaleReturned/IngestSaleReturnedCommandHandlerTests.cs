using Moq;
using Notifications.Application.Contracts.Infrastructure;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Features.Ingestion.Commands.IngestSaleReturned;
using Notifications.Application.Models;
using Notifications.Domain.Entities;
using Xunit;

namespace Notifications.Application.Tests.Features.Ingestion.Commands.IngestSaleReturned
{
    public class IngestSaleReturnedCommandHandlerTests
    {
        private readonly Mock<INotificationRepository> _notificationRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<INotificationPusher> _notificationPusher = new();
        private readonly IngestSaleReturnedCommandHandler _sut;

        public IngestSaleReturnedCommandHandlerTests()
        {
            _sut = new IngestSaleReturnedCommandHandler(
                _notificationRepository.Object,
                _unitOfWork.Object,
                _notificationPusher.Object);
        }

        [Fact]
        public async Task Handle_NewSaleReturn_CreatesNotificationAndPushesIt()
        {
            _notificationRepository.Setup(r => r.ExistsForSaleReturn(42)).ReturnsAsync(false);
            _notificationRepository
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => { n.Id = 9; return n; });

            var result = await _sut.Handle(new IngestSaleReturnedCommand { SaleId = 42, Total = 50.00m }, CancellationToken.None);

            Assert.False(result.AlreadyProcessed);
            _notificationRepository.Verify(r => r.AddAsync(It.Is<Notification>(n =>
                n.Type == NotificationType.SaleReturned &&
                n.SourceSaleReturnId == 42 &&
                n.SourceSaleId == null)), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _notificationPusher.Verify(p => p.PushAsync(
                It.Is<NotificationDto>(dto => dto.Id == 9),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_SaleReturnAlreadyIngested_ReturnsAlreadyProcessedWithoutCreatingOrPushing()
        {
            _notificationRepository.Setup(r => r.ExistsForSaleReturn(42)).ReturnsAsync(true);

            var result = await _sut.Handle(new IngestSaleReturnedCommand { SaleId = 42, Total = 50.00m }, CancellationToken.None);

            Assert.True(result.AlreadyProcessed);
            _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
            _notificationPusher.Verify(p => p.PushAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SameSaleIdAsAnExistingSaleCompletedNotification_StillIngestsReturn()
        {
            // SourceSaleId and SourceSaleReturnId are independent dedup keys (README E1) —
            // ExistsForSaleReturn, not ExistsForSale, gates this handler.
            _notificationRepository.Setup(r => r.ExistsForSaleReturn(42)).ReturnsAsync(false);
            _notificationRepository
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => n);

            var result = await _sut.Handle(new IngestSaleReturnedCommand { SaleId = 42, Total = 50.00m }, CancellationToken.None);

            Assert.False(result.AlreadyProcessed);
            _notificationRepository.Verify(r => r.ExistsForSale(It.IsAny<int>()), Times.Never);
        }
    }
}

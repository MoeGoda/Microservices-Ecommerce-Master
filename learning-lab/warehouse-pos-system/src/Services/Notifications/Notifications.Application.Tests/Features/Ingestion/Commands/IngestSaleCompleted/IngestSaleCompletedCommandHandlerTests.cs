using Moq;
using Notifications.Application.Contracts.Infrastructure;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Features.Ingestion.Commands.IngestSaleCompleted;
using Notifications.Application.Models;
using Notifications.Domain.Entities;
using Xunit;

namespace Notifications.Application.Tests.Features.Ingestion.Commands.IngestSaleCompleted
{
    public class IngestSaleCompletedCommandHandlerTests
    {
        private readonly Mock<INotificationRepository> _notificationRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<INotificationPusher> _notificationPusher = new();
        private readonly IngestSaleCompletedCommandHandler _sut;

        public IngestSaleCompletedCommandHandlerTests()
        {
            _sut = new IngestSaleCompletedCommandHandler(
                _notificationRepository.Object,
                _unitOfWork.Object,
                _notificationPusher.Object);
        }

        [Fact]
        public async Task Handle_NewSale_CreatesNotificationAndPushesIt()
        {
            _notificationRepository.Setup(r => r.ExistsForSale(42)).ReturnsAsync(false);
            _notificationRepository
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => { n.Id = 7; return n; });

            var result = await _sut.Handle(new IngestSaleCompletedCommand { SaleId = 42, Total = 199.99m }, CancellationToken.None);

            Assert.False(result.AlreadyProcessed);
            _notificationRepository.Verify(r => r.AddAsync(It.Is<Notification>(n =>
                n.Type == NotificationType.SaleCompleted &&
                n.SourceSaleId == 42 &&
                n.Message.Contains("42") &&
                n.Message.Contains("199.99"))), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _notificationPusher.Verify(p => p.PushAsync(
                It.Is<NotificationDto>(dto => dto.Id == 7),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_SaleAlreadyIngested_ReturnsAlreadyProcessedWithoutCreatingOrPushing()
        {
            _notificationRepository.Setup(r => r.ExistsForSale(42)).ReturnsAsync(true);

            var result = await _sut.Handle(new IngestSaleCompletedCommand { SaleId = 42, Total = 199.99m }, CancellationToken.None);

            Assert.True(result.AlreadyProcessed);
            _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
            _notificationPusher.Verify(p => p.PushAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

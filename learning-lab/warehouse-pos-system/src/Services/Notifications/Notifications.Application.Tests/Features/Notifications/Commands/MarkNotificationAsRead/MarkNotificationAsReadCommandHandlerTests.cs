using Common.Exceptions;
using Moq;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using Notifications.Domain.Entities;
using Xunit;

namespace Notifications.Application.Tests.Features.Notifications.Commands.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommandHandlerTests
    {
        private readonly Mock<INotificationRepository> _notificationRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly MarkNotificationAsReadCommandHandler _sut;

        public MarkNotificationAsReadCommandHandlerTests()
        {
            _sut = new MarkNotificationAsReadCommandHandler(_notificationRepository.Object, _unitOfWork.Object);
        }

        [Fact]
        public async Task Handle_ExistingUnreadNotification_MarksItReadAndReturnsDto()
        {
            var notification = new Notification { Id = 5, Type = NotificationType.SaleCompleted, Message = "Sale #5 completed.", IsRead = false };
            _notificationRepository.Setup(r => r.GetById(5)).ReturnsAsync(notification);

            var dto = await _sut.Handle(new MarkNotificationAsReadCommand { Id = 5 }, CancellationToken.None);

            Assert.True(dto.IsRead);
            Assert.True(notification.IsRead);
            _notificationRepository.Verify(r => r.UpdateAsync(notification), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_NotificationDoesNotExist_ThrowsNotFoundExceptionWithoutSaving()
        {
            _notificationRepository.Setup(r => r.GetById(99)).ReturnsAsync((Notification?)null);

            await Assert.ThrowsAsync<NotFoundException>(
                () => _sut.Handle(new MarkNotificationAsReadCommand { Id = 99 }, CancellationToken.None));

            _notificationRepository.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_NotificationAlreadyRead_StaysReadAndStillSaves()
        {
            var notification = new Notification { Id = 5, Type = NotificationType.SaleCompleted, Message = "Sale #5 completed.", IsRead = true };
            _notificationRepository.Setup(r => r.GetById(5)).ReturnsAsync(notification);

            var dto = await _sut.Handle(new MarkNotificationAsReadCommand { Id = 5 }, CancellationToken.None);

            Assert.True(dto.IsRead);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}

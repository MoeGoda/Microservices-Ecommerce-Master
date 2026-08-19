using Moq;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using Notifications.Domain.Entities;
using Xunit;

namespace Notifications.Application.Tests.Features.Notifications.Commands.MarkAllNotificationsAsRead
{
    public class MarkAllNotificationsAsReadCommandHandlerTests
    {
        private readonly Mock<INotificationRepository> _notificationRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly MarkAllNotificationsAsReadCommandHandler _sut;

        public MarkAllNotificationsAsReadCommandHandlerTests()
        {
            _sut = new MarkAllNotificationsAsReadCommandHandler(_notificationRepository.Object, _unitOfWork.Object);
        }

        [Fact]
        public async Task Handle_SomeUnreadNotifications_MarksEachReadAndReturnsCount()
        {
            var unread = new List<Notification>
            {
                new() { Id = 1, IsRead = false },
                new() { Id = 2, IsRead = false },
                new() { Id = 3, IsRead = false },
            };
            _notificationRepository.Setup(r => r.GetUnread()).ReturnsAsync(unread);

            var count = await _sut.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);

            Assert.Equal(3, count);
            Assert.All(unread, n => Assert.True(n.IsRead));
            _notificationRepository.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Exactly(3));
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_NoUnreadNotifications_ReturnsZeroWithoutSaving()
        {
            _notificationRepository.Setup(r => r.GetUnread()).ReturnsAsync(Enumerable.Empty<Notification>());

            var count = await _sut.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);

            Assert.Equal(0, count);
            _notificationRepository.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }
}

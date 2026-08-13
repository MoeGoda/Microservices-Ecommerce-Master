using Common.Exceptions;
using MediatR;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Models;
using Notifications.Domain.Entities;

namespace Notifications.Application.Features.Notifications.Commands.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, NotificationDto>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MarkNotificationAsReadCommandHandler(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<NotificationDto> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetById(request.Id)
                ?? throw new NotFoundException(nameof(Notification), request.Id);

            notification.IsRead = true;
            await _notificationRepository.UpdateAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            return NotificationDto.FromEntity(notification);
        }
    }
}

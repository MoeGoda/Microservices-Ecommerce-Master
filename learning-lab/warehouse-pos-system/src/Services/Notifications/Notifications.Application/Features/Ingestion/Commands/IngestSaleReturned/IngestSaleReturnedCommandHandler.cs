using MediatR;
using Notifications.Application.Contracts.Infrastructure;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Models;
using Notifications.Domain.Entities;

namespace Notifications.Application.Features.Ingestion.Commands.IngestSaleReturned
{
    public class IngestSaleReturnedCommandHandler : IRequestHandler<IngestSaleReturnedCommand, IngestResultDto>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationPusher _notificationPusher;

        public IngestSaleReturnedCommandHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            INotificationPusher notificationPusher)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _notificationPusher = notificationPusher;
        }

        public async Task<IngestResultDto> Handle(IngestSaleReturnedCommand request, CancellationToken cancellationToken)
        {
            // Own dedup key (SourceSaleReturnId) — see that column's own
            // comment for why it can't share SourceSaleId's.
            if (await _notificationRepository.ExistsForSaleReturn(request.SaleId))
            {
                return new IngestResultDto { AlreadyProcessed = true };
            }

            var notification = await _notificationRepository.AddAsync(new Notification
            {
                Type = NotificationType.SaleReturned,
                Message = $"Sale #{request.SaleId} returned — total {request.Total:0.00}.",
                SourceSaleReturnId = request.SaleId,
            });

            await _unitOfWork.SaveChangesAsync();

            await _notificationPusher.PushAsync(NotificationDto.FromEntity(notification), cancellationToken);

            return new IngestResultDto { AlreadyProcessed = false };
        }
    }
}

using MediatR;
using Notifications.Application.Contracts.Infrastructure;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Models;
using Notifications.Domain.Entities;

namespace Notifications.Application.Features.Ingestion.Commands.IngestSaleCompleted
{
    public class IngestSaleCompletedCommandHandler : IRequestHandler<IngestSaleCompletedCommand, IngestResultDto>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationPusher _notificationPusher;

        public IngestSaleCompletedCommandHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            INotificationPusher notificationPusher)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _notificationPusher = notificationPusher;
        }

        public async Task<IngestResultDto> Handle(IngestSaleCompletedCommand request, CancellationToken cancellationToken)
        {
            // The inbox check: at-least-once delivery (POS's outbox,
            // C3/D1) means this can arrive more than once for the same
            // sale — a repeat delivery is a no-op, not a second toast for
            // a sale the user already saw completed.
            if (await _notificationRepository.ExistsForSale(request.SaleId))
            {
                return new IngestResultDto { AlreadyProcessed = true };
            }

            var notification = await _notificationRepository.AddAsync(new Notification
            {
                Type = NotificationType.SaleCompleted,
                Message = $"Sale #{request.SaleId} completed — total {request.Total:0.00}.",
                SourceSaleId = request.SaleId,
            });

            // SaveChangesAsync first — the push carries the real,
            // database-assigned Id, not a client-side placeholder.
            await _unitOfWork.SaveChangesAsync();

            await _notificationPusher.PushAsync(NotificationDto.FromEntity(notification), cancellationToken);

            return new IngestResultDto { AlreadyProcessed = false };
        }
    }
}

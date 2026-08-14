using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.PurchaseOrders.Commands.CancelPurchaseOrder
{
    public class CancelPurchaseOrderCommandHandler : IRequestHandler<CancelPurchaseOrderCommand, PurchaseOrderDetailDto>
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CancelPurchaseOrderCommandHandler(IPurchaseOrderRepository purchaseOrderRepository, IUnitOfWork unitOfWork)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PurchaseOrderDetailDto> Handle(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _purchaseOrderRepository.GetById(request.PurchaseOrderId)
                ?? throw new NotFoundException(nameof(PurchaseOrder), request.PurchaseOrderId);

            // Cancelling after ANY receipt would leave stock on the shelf
            // with no order behind it any more — the same "don't erase a
            // fact that already happened" reasoning ReturnSaleCommand
            // relies on for a Completed sale. Draft and Ordered-with-
            // nothing-received-yet are the only safe states to cancel
            // from; PartiallyReceived/Received/Cancelled all reject it.
            if (order.Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Ordered))
            {
                throw new ConflictException($"Purchase order '{order.OrderNumber}' is {order.Status} — only a Draft or fully-unreceived Ordered order can be cancelled.");
            }

            order.Status = PurchaseOrderStatus.Cancelled;
            await _purchaseOrderRepository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            return PurchaseOrderDetailDto.FromEntity(order);
        }
    }
}

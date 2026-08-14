using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.PurchaseOrders.Commands.SubmitPurchaseOrder
{
    public class SubmitPurchaseOrderCommandHandler : IRequestHandler<SubmitPurchaseOrderCommand, PurchaseOrderDetailDto>
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SubmitPurchaseOrderCommandHandler(IPurchaseOrderRepository purchaseOrderRepository, IUnitOfWork unitOfWork)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PurchaseOrderDetailDto> Handle(SubmitPurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _purchaseOrderRepository.GetById(request.PurchaseOrderId)
                ?? throw new NotFoundException(nameof(PurchaseOrder), request.PurchaseOrderId);

            if (order.Status != PurchaseOrderStatus.Draft)
            {
                throw new ConflictException($"Purchase order '{order.OrderNumber}' is {order.Status}, not Draft — it can't be submitted again.");
            }

            order.Status = PurchaseOrderStatus.Ordered;
            order.OrderedAtUtc = DateTime.UtcNow;
            await _purchaseOrderRepository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            return PurchaseOrderDetailDto.FromEntity(order);
        }
    }
}

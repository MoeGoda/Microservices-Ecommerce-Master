using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.PurchaseOrders.Queries.GetPurchaseOrderById
{
    public class GetPurchaseOrderByIdQueryHandler : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDetailDto>
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;

        public GetPurchaseOrderByIdQueryHandler(IPurchaseOrderRepository purchaseOrderRepository)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
        }

        public async Task<PurchaseOrderDetailDto> Handle(GetPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var order = await _purchaseOrderRepository.GetById(request.Id)
                ?? throw new NotFoundException(nameof(PurchaseOrder), request.Id);

            return PurchaseOrderDetailDto.FromEntity(order);
        }
    }
}

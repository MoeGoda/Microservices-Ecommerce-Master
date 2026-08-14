using Common.Pagination;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders
{
    public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, PagedResult<PurchaseOrderSummaryDto>>
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;

        public GetPurchaseOrdersQueryHandler(IPurchaseOrderRepository purchaseOrderRepository)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
        }

        public async Task<PagedResult<PurchaseOrderSummaryDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
        {
            var (orders, totalCount) = await _purchaseOrderRepository.GetPaged(request.Page, request.PageSize);
            var dtos = orders.Select(PurchaseOrderSummaryDto.FromEntity).ToList();
            return PagedResult<PurchaseOrderSummaryDto>.Create(dtos, request.Page, request.PageSize, totalCount);
        }
    }
}

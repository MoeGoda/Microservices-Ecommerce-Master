using Common.Pagination;
using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders
{
    public class GetPurchaseOrdersQuery : IRequest<PagedResult<PurchaseOrderSummaryDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

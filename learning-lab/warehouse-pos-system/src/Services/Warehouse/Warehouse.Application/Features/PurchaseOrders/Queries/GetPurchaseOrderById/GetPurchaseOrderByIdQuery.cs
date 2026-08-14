using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.PurchaseOrders.Queries.GetPurchaseOrderById
{
    public class GetPurchaseOrderByIdQuery : IRequest<PurchaseOrderDetailDto>
    {
        public int Id { get; set; }
    }
}

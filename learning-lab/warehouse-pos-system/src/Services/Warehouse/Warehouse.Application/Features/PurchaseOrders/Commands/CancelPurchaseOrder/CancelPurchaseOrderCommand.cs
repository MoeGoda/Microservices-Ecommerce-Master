using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.PurchaseOrders.Commands.CancelPurchaseOrder
{
    public class CancelPurchaseOrderCommand : IRequest<PurchaseOrderDetailDto>
    {
        public int PurchaseOrderId { get; set; }
    }
}

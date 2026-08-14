using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.PurchaseOrders.Commands.SubmitPurchaseOrder
{
    // Draft -> Ordered. The line between the two the whole module is
    // built around: everything about WHAT was ordered can only change
    // before this point.
    public class SubmitPurchaseOrderCommand : IRequest<PurchaseOrderDetailDto>
    {
        public int PurchaseOrderId { get; set; }
    }
}

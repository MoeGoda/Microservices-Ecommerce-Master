using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.PurchaseOrders.Commands.ReceivePurchaseOrderLine
{
    // The PO-aware sibling of ReceiveStockCommand — that command stays
    // completely untouched for free-text restocking; this one is the
    // ONLY thing that increments PurchaseOrderLine.ReceivedQuantity and
    // moves a PurchaseOrder toward PartiallyReceived/Received. Quantity
    // is expressed in the LINE's own UnitOfMeasureId (see
    // PurchaseOrderLine), converted to the item's base unit the same way
    // ReceiveStockCommandHandler does before it ever touches StockLevel.
    public class ReceivePurchaseOrderLineCommand : IRequest<PurchaseOrderDetailDto>
    {
        public int PurchaseOrderId { get; set; }
        public int PurchaseOrderLineId { get; set; }
        public int LocationId { get; set; }
        public decimal Quantity { get; set; }

        // Defaults to the order's own OrderNumber if left blank — see the
        // handler. Free-form otherwise, same as StockTransaction.Reference.
        public string? Reference { get; set; }
    }
}

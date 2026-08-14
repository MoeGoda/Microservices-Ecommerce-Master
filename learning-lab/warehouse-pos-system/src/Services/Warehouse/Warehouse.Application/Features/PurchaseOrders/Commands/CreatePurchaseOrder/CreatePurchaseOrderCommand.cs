using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder
{
    // A PurchaseOrder is created with all of its lines at once — the same
    // "everything the first save needs, in one call" shape
    // CreateItemCommand already uses for an item and its first barcode.
    // Lines can no longer be added or removed once the order leaves
    // Draft (SubmitPurchaseOrderCommand locks it) — get them right here,
    // or cancel the Draft and start a new one.
    public class CreatePurchaseOrderCommand : IRequest<PurchaseOrderDetailDto>
    {
        public int SupplierId { get; set; }
        public string? Notes { get; set; }

        // Set by the controller from the caller's own JWT claim, never
        // from the request body — the same "context is authoritative
        // over the body" idiom SalesController.Start already uses for
        // CashierUserId.
        public int CreatedByUserId { get; set; }

        public List<CreatePurchaseOrderLineRequest> Lines { get; set; } = new();
    }

    public class CreatePurchaseOrderLineRequest
    {
        public int ItemId { get; set; }
        public int UnitOfMeasureId { get; set; }
        public decimal OrderedQuantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}

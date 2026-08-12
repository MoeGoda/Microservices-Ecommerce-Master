using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Commands.AddSaleLine
{
    // Sku/ItemName/UnitPrice are supplied by the caller, not looked up
    // here — this command trusts that whatever called it already resolved
    // the real, current values for ItemId (Warehouse's catalog, B1/B2).
    // Right now nothing enforces that trust; Step C2 is exactly where a
    // real Warehouse lookup gets inserted in front of this call so the
    // values passed in are actually verified rather than merely assumed.
    public class AddSaleLineCommand : IRequest<SaleDto>
    {
        public int SaleId { get; set; }
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}

using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Commands.AddSaleLine
{
    // The shape C1's own README predicted: this used to accept
    // Sku/ItemName/UnitPrice directly from the caller, trusted with no
    // verification. Step C2 replaces that trust with a real check — the
    // handler resolves Barcode against Warehouse's catalog and checks
    // stock at the sale's own LocationId before ever writing a SaleLine.
    public class AddSaleLineCommand : IRequest<SaleDto>
    {
        public int SaleId { get; set; }
        public string Barcode { get; set; } = null!;
        public int Quantity { get; set; }

        // Cashier-entered — "Line discount" in the register's action
        // panel. Only honored when Warehouse didn't already resolve an
        // active promotion for this item (see the handler); the two
        // never stack.
        public decimal? ManualDiscountPercent { get; set; }
    }
}

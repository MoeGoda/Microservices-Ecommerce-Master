using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Stock.Commands.ReceiveStock
{
    // "Stock came in" — the handler that finally makes good on B1's
    // promise that a StockLevel change and its StockTransaction always
    // commit together. Quantity is expressed in WHATEVER unit the goods
    // arrived in (UnitOfMeasureId) — the handler converts to the item's
    // base unit via ItemUnit.ConversionFactor before touching StockLevel;
    // pass the item's own BaseUnitOfMeasureId here to receive directly in
    // the base unit with no conversion.
    public class ReceiveStockCommand : IRequest<StockLevelDto>
    {
        public int ItemId { get; set; }
        public int LocationId { get; set; }
        public decimal Quantity { get; set; }
        public int UnitOfMeasureId { get; set; }

        // Free-form, deliberately not a foreign key — e.g. a purchase
        // order number. See StockTransaction.Reference.
        public string? Reference { get; set; }
    }
}

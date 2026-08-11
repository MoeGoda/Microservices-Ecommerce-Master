using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Commands.AddItemUnit
{
    // Defines an ALTERNATE unit an item can be received/sold in, e.g.
    // "1 BOX of this item = 12 of its base unit." Not for the base unit
    // itself — that's just Item.BaseUnitOfMeasureId, an implicit factor of 1.
    public class AddItemUnitCommand : IRequest<ItemUnitDto>
    {
        public int ItemId { get; set; }
        public int UnitOfMeasureId { get; set; }
        public decimal ConversionFactor { get; set; }
    }
}

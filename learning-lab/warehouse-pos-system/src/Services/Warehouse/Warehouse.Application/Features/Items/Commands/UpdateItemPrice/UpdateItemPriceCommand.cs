using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Commands.UpdateItemPrice
{
    // The only sanctioned way to change Item.UnitPrice after creation —
    // there is deliberately no generic "edit item" command that happens to
    // let UnitPrice through unnoticed, because every price change needs an
    // ItemPriceHistory row recorded alongside it (see the handler). Going
    // through this command is what makes that guarantee hold.
    public class UpdateItemPriceCommand : IRequest<ItemDetailDto>
    {
        public int ItemId { get; set; }
        public decimal NewPrice { get; set; }
    }
}

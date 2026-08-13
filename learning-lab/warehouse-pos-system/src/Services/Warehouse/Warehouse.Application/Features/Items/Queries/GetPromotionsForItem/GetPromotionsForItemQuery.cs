using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.GetPromotionsForItem
{
    // Every promotion this item has ever had — active, upcoming, expired,
    // or cancelled — as opposed to EffectivePriceResolver's
    // GetActiveForItem, which only ever cares about "what applies right
    // now." This is the browse/history view the C5 README named as a real
    // gap; CancelPromotionCommand is what an admin acts on from it.
    public class GetPromotionsForItemQuery : IRequest<IEnumerable<PromotionDto>>
    {
        public int ItemId { get; set; }
    }
}

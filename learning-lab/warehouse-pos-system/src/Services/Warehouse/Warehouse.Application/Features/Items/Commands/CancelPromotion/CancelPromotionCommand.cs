using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Commands.CancelPromotion
{
    // Cancelling doesn't delete the row or touch StartsAtUtc/EndsAtUtc —
    // it just excludes this promotion from GetActiveForItem from this
    // point on, immediately, regardless of where "now" falls in its
    // original window. The original window stays as historical record.
    public class CancelPromotionCommand : IRequest<PromotionDto>
    {
        public int ItemId { get; set; }
        public int PromotionId { get; set; }
    }
}

using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.GetItemPriceHistory
{
    public class GetItemPriceHistoryQuery : IRequest<IEnumerable<ItemPriceHistoryDto>>
    {
        public int ItemId { get; set; }
    }
}

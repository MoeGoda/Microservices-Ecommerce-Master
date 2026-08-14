using Common.Pagination;
using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.GetAllItems
{
    // F1 — real pagination, not the flat "top N" idiom
    // GetTopSellingItemsQuery/GetRecentNotificationsQuery already used:
    // this is THE catalog browse list, the one place in this system most
    // likely to actually grow past a screenful over a real deployment's
    // lifetime. Page/PageSize default to the same values the Angular
    // client's own paginator starts on.
    public class GetAllItemsQuery : IRequest<PagedResult<ItemSummaryDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

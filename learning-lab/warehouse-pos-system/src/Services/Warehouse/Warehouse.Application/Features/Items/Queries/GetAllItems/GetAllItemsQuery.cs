using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.GetAllItems
{
    public class GetAllItemsQuery : IRequest<IEnumerable<ItemSummaryDto>>
    {
    }
}

using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.GetItemVariants
{
    // "What pack variants exist for this base product" — e.g. every
    // Item whose ParentItemId points at a given base Item.
    public class GetItemVariantsQuery : IRequest<IEnumerable<ItemSummaryDto>>
    {
        public int ParentItemId { get; set; }
    }
}

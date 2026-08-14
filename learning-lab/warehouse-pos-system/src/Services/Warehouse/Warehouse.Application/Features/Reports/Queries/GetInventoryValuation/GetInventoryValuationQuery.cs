using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Reports.Queries.GetInventoryValuation
{
    // J — the first report living directly on Warehouse rather than
    // Reporting: unlike sales-by-day/top-selling (facts about EVENTS
    // across services), this is a live view of Warehouse's OWN current
    // state (StockLevel x Item.UnitPrice) — there's nothing here that
    // isn't already sitting in Warehouse's own database right now, so
    // querying it live is simpler and more current than fanning it out
    // as yet another event just to duplicate it into a Reporting read
    // model.
    public class GetInventoryValuationQuery : IRequest<IEnumerable<InventoryValuationLineDto>>
    {
    }
}

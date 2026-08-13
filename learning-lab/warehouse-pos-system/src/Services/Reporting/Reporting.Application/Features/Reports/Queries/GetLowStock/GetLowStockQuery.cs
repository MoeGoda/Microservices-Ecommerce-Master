using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetLowStock
{
    // Every StockLevelRecord at or below its own ReorderThreshold —
    // exactly the "should fire a LowStockEvent" condition StockLevel's
    // own comment (Warehouse, B1) named as future E1 territory, answered
    // here as a report instead, from Reporting's own read model.
    public class GetLowStockQuery : IRequest<IEnumerable<StockLevelRecordDto>>
    {
    }
}

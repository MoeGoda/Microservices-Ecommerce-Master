using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetStockLevels
{
    // Every StockLevelRecord ingested so far — same "raw read model, not
    // a report" reasoning as GetSalesQuery.
    public class GetStockLevelsQuery : IRequest<IEnumerable<StockLevelRecordDto>>
    {
    }
}

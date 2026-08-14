using Common.Pagination;
using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetStockMovements
{
    // J — the real report over StockMovementRecord, as opposed to a raw
    // ReadModels dump (there isn't one for this table — every field here
    // already IS what a real report needs, unlike SaleRecord's own raw
    // dump vs. sales-by-day split).
    public class GetStockMovementsQuery : IRequest<PagedResult<StockMovementRecordDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public int? ItemId { get; set; }
        public int? LocationId { get; set; }
    }
}

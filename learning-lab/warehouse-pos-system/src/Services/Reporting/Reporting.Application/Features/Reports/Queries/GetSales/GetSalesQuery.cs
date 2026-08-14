using Common.Pagination;
using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetSales
{
    // Every SaleRecord ingested so far, unfiltered — a raw read-model
    // dump proving ingestion actually worked, NOT a report (D2's real
    // aggregations live in ReportsController instead). F1 adds real
    // paging here for the same reason Warehouse's GetAllItemsQuery got
    // it: this is a raw, ever-growing table dump with no natural upper
    // bound the way an aggregated report (sales-by-day, top-selling) has.
    public class GetSalesQuery : IRequest<PagedResult<SaleRecordDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

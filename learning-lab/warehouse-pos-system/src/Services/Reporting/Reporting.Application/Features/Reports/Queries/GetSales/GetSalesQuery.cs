using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetSales
{
    // Every SaleRecord ingested so far, unfiltered — a raw read-model
    // dump proving ingestion actually worked, NOT a report. D2 builds the
    // real aggregations (sales by day, top-selling) on top of this same
    // read model; this step only has to prove the read model itself is
    // correct and queryable.
    public class GetSalesQuery : IRequest<IEnumerable<SaleRecordDto>>
    {
    }
}

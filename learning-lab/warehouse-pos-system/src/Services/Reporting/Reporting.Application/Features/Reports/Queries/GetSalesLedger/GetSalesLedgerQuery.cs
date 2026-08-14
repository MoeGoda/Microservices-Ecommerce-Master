using Common.Pagination;
using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetSalesLedger
{
    public class GetSalesLedgerQuery : IRequest<PagedResult<SalesLedgerEntryDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
    }
}

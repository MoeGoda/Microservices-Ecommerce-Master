using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetCashierPerformance
{
    public class GetCashierPerformanceQuery : IRequest<IEnumerable<CashierPerformanceDto>>
    {
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
    }
}

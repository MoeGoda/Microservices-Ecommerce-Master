using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetSalesByDay
{
    // The first REAL report (D2), as opposed to D1's raw ReadModels dump —
    // one row per day that had at least one sale, aggregated straight in
    // the database (ISaleRecordRepository.GetSalesByDay).
    public class GetSalesByDayQuery : IRequest<IEnumerable<SalesByDayDto>>
    {
    }
}

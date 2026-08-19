using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.CashDrawer.Queries.GetCashDrawerXReport
{
    // A read-only mid-shift snapshot — see CashDrawerXReportDto for why
    // it never closes the session itself.
    public class GetCashDrawerXReportQuery : IRequest<CashDrawerXReportDto>
    {
        public int SessionId { get; set; }
    }
}

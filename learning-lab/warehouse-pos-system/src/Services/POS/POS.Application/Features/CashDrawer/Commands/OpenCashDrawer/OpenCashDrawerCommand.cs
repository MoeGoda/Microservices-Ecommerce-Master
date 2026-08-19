using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.CashDrawer.Commands.OpenCashDrawer
{
    public class OpenCashDrawerCommand : IRequest<CashDrawerSessionDto>
    {
        public int LocationId { get; set; }
        public int CashierUserId { get; set; }
        public decimal OpeningFloat { get; set; }
    }
}

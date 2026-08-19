using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.CashDrawer.Commands.CloseCashDrawer
{
    public class CloseCashDrawerCommand : IRequest<CashDrawerSessionDto>
    {
        public int SessionId { get; set; }
        public decimal ClosingCount { get; set; }
    }
}

using MediatR;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.CashDrawer.Commands.RecordCashMovement
{
    // The register's "Cash In" / "Cash Out" buttons — a manual movement
    // against the currently open session at that location, not tied to a
    // specific Sale.
    public class RecordCashMovementCommand : IRequest<CashMovementDto>
    {
        public int LocationId { get; set; }
        public CashMovementType Type { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = null!;
        public int CreatedByUserId { get; set; }
    }
}

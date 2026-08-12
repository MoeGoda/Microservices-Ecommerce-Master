using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Commands.StartSale
{
    // Opens a new basket at a register. LocationId/CashierUserId are
    // trusted as given here — validating that they refer to a real
    // Warehouse.Location / Identity.User is exactly the kind of
    // cross-service check Step C2 introduces for barcodes; nothing in
    // this step reaches out to either service yet.
    public class StartSaleCommand : IRequest<SaleDto>
    {
        public int LocationId { get; set; }
        public int CashierUserId { get; set; }
    }
}

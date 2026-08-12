using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Commands.Checkout
{
    // Finalizes a sale — the state transition Step C3 hangs a
    // SaleCompleted event off of, to actually decrement Warehouse stock.
    // Nothing here raises that event yet; this step only gets the sale
    // itself to Completed. See Sale.cs.
    public class CheckoutCommand : IRequest<SaleDto>
    {
        public int SaleId { get; set; }
    }
}

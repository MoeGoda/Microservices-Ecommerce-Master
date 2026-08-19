using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Commands.SetSaleCustomer
{
    // Attaches (or, with a null CustomerId, detaches) a Customer on an
    // in-progress sale — the register's "customer search" control. Doing
    // this before checkout is what makes Checkout's loyalty-points
    // accrual possible; a walk-in sale with no customer attached simply
    // earns nothing.
    public class SetSaleCustomerCommand : IRequest<SaleDto>
    {
        public int SaleId { get; set; }
        public int? CustomerId { get; set; }
    }
}

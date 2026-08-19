using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Customers.Commands.AdjustCustomerBalance
{
    // A manual store-credit/tab adjustment — not a payment posting, not an
    // AR ledger entry. Delta can be positive (credit the customer) or
    // negative (debit them); Reason is required so every adjustment is
    // at least self-explaining in the absence of a real ledger.
    public class AdjustCustomerBalanceCommand : IRequest<CustomerDto>
    {
        public int CustomerId { get; set; }
        public decimal Delta { get; set; }
        public string Reason { get; set; } = null!;
    }
}

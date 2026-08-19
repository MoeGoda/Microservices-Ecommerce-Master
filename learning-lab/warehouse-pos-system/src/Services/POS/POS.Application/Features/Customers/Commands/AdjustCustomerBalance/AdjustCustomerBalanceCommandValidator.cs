using FluentValidation;

namespace POS.Application.Features.Customers.Commands.AdjustCustomerBalance
{
    public class AdjustCustomerBalanceCommandValidator : AbstractValidator<AdjustCustomerBalanceCommand>
    {
        public AdjustCustomerBalanceCommandValidator()
        {
            RuleFor(c => c.CustomerId).GreaterThan(0);
            RuleFor(c => c.Delta).NotEqual(0);
            RuleFor(c => c.Reason).NotEmpty().MaximumLength(200);
        }
    }
}

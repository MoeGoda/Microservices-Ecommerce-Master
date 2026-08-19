using FluentValidation;

namespace POS.Application.Features.CashDrawer.Commands.OpenCashDrawer
{
    public class OpenCashDrawerCommandValidator : AbstractValidator<OpenCashDrawerCommand>
    {
        public OpenCashDrawerCommandValidator()
        {
            RuleFor(c => c.LocationId).GreaterThan(0);
            RuleFor(c => c.CashierUserId).GreaterThan(0);
            RuleFor(c => c.OpeningFloat).GreaterThanOrEqualTo(0);
        }
    }
}

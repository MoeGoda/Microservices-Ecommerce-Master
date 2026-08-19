using FluentValidation;

namespace POS.Application.Features.CashDrawer.Commands.CloseCashDrawer
{
    public class CloseCashDrawerCommandValidator : AbstractValidator<CloseCashDrawerCommand>
    {
        public CloseCashDrawerCommandValidator()
        {
            RuleFor(c => c.SessionId).GreaterThan(0);
            RuleFor(c => c.ClosingCount).GreaterThanOrEqualTo(0);
        }
    }
}

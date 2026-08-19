using FluentValidation;

namespace POS.Application.Features.Sales.Commands.SetTaxExempt
{
    public class SetTaxExemptCommandValidator : AbstractValidator<SetTaxExemptCommand>
    {
        public SetTaxExemptCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
        }
    }
}

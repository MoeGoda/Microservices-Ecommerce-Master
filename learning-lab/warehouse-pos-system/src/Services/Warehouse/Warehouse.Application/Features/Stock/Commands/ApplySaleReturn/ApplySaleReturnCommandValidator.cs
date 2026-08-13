using FluentValidation;

namespace Warehouse.Application.Features.Stock.Commands.ApplySaleReturn
{
    public class ApplySaleReturnCommandValidator : AbstractValidator<ApplySaleReturnCommand>
    {
        public ApplySaleReturnCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
            RuleFor(c => c.LocationId).GreaterThan(0);
            RuleFor(c => c.Lines).NotEmpty();
            RuleForEach(c => c.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.ItemId).GreaterThan(0);
                line.RuleFor(l => l.Quantity).GreaterThan(0);
            });
        }
    }
}

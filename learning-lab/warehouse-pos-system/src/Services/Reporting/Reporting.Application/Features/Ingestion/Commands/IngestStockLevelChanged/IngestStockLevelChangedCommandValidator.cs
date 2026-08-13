using FluentValidation;

namespace Reporting.Application.Features.Ingestion.Commands.IngestStockLevelChanged
{
    public class IngestStockLevelChangedCommandValidator : AbstractValidator<IngestStockLevelChangedCommand>
    {
        public IngestStockLevelChangedCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.LocationId).GreaterThan(0);
            RuleFor(c => c.QuantityOnHand).GreaterThanOrEqualTo(0);
        }
    }
}

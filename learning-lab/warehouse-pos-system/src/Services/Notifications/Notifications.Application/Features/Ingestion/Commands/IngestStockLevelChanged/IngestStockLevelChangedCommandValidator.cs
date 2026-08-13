using FluentValidation;

namespace Notifications.Application.Features.Ingestion.Commands.IngestStockLevelChanged
{
    public class IngestStockLevelChangedCommandValidator : AbstractValidator<IngestStockLevelChangedCommand>
    {
        public IngestStockLevelChangedCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.Sku).NotEmpty();
            RuleFor(c => c.ItemName).NotEmpty();
            RuleFor(c => c.LocationId).GreaterThan(0);
            RuleFor(c => c.LocationCode).NotEmpty();
            RuleFor(c => c.LocationName).NotEmpty();
            RuleFor(c => c.QuantityOnHand).GreaterThanOrEqualTo(0);
            RuleFor(c => c.ReorderThreshold).GreaterThanOrEqualTo(0);
        }
    }
}

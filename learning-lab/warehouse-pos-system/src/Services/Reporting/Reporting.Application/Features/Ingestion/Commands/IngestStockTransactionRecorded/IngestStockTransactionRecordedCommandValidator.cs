using FluentValidation;

namespace Reporting.Application.Features.Ingestion.Commands.IngestStockTransactionRecorded
{
    public class IngestStockTransactionRecordedCommandValidator : AbstractValidator<IngestStockTransactionRecordedCommand>
    {
        public IngestStockTransactionRecordedCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.Sku).NotEmpty();
            RuleFor(c => c.ItemName).NotEmpty();
            RuleFor(c => c.LocationId).GreaterThan(0);
            RuleFor(c => c.LocationCode).NotEmpty();
            RuleFor(c => c.LocationName).NotEmpty();
            RuleFor(c => c.QuantityChange).NotEqual(0);
            RuleFor(c => c.Reason).NotEmpty();
        }
    }
}

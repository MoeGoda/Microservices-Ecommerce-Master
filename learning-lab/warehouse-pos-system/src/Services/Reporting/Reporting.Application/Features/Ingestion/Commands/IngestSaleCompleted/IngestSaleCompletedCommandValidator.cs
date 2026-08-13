using FluentValidation;

namespace Reporting.Application.Features.Ingestion.Commands.IngestSaleCompleted
{
    public class IngestSaleCompletedCommandValidator : AbstractValidator<IngestSaleCompletedCommand>
    {
        public IngestSaleCompletedCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
            RuleFor(c => c.LocationId).GreaterThan(0);
            RuleFor(c => c.CashierUserId).GreaterThan(0);
            RuleFor(c => c.Total).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Lines).NotEmpty();
            RuleForEach(c => c.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.ItemId).GreaterThan(0);
                line.RuleFor(l => l.Sku).NotEmpty();
                line.RuleFor(l => l.ItemName).NotEmpty();
                line.RuleFor(l => l.Quantity).GreaterThan(0);
            });
        }
    }
}

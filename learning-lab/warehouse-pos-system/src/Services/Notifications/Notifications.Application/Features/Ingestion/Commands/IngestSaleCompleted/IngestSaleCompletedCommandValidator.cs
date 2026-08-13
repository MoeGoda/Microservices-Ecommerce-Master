using FluentValidation;

namespace Notifications.Application.Features.Ingestion.Commands.IngestSaleCompleted
{
    public class IngestSaleCompletedCommandValidator : AbstractValidator<IngestSaleCompletedCommand>
    {
        public IngestSaleCompletedCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
            RuleFor(c => c.Total).GreaterThanOrEqualTo(0);
        }
    }
}

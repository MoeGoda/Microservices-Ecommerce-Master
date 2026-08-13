using FluentValidation;

namespace Notifications.Application.Features.Ingestion.Commands.IngestSaleReturned
{
    public class IngestSaleReturnedCommandValidator : AbstractValidator<IngestSaleReturnedCommand>
    {
        public IngestSaleReturnedCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
            RuleFor(c => c.Total).GreaterThanOrEqualTo(0);
        }
    }
}

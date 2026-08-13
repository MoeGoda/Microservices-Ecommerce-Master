using FluentValidation;

namespace Reporting.Application.Features.Ingestion.Commands.IngestSaleReturned
{
    public class IngestSaleReturnedCommandValidator : AbstractValidator<IngestSaleReturnedCommand>
    {
        public IngestSaleReturnedCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
        }
    }
}

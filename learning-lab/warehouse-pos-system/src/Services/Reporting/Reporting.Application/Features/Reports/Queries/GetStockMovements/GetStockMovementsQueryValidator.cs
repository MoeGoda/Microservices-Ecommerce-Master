using FluentValidation;

namespace Reporting.Application.Features.Reports.Queries.GetStockMovements
{
    public class GetStockMovementsQueryValidator : AbstractValidator<GetStockMovementsQuery>
    {
        public GetStockMovementsQueryValidator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
            RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
            RuleFor(q => q.ToUtc).GreaterThanOrEqualTo(q => q.FromUtc).When(q => q.FromUtc.HasValue && q.ToUtc.HasValue);
        }
    }
}

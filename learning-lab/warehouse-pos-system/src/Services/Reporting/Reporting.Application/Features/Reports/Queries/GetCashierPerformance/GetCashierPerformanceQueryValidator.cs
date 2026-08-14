using FluentValidation;

namespace Reporting.Application.Features.Reports.Queries.GetCashierPerformance
{
    public class GetCashierPerformanceQueryValidator : AbstractValidator<GetCashierPerformanceQuery>
    {
        public GetCashierPerformanceQueryValidator()
        {
            RuleFor(q => q.ToUtc).GreaterThanOrEqualTo(q => q.FromUtc).When(q => q.FromUtc.HasValue && q.ToUtc.HasValue);
        }
    }
}

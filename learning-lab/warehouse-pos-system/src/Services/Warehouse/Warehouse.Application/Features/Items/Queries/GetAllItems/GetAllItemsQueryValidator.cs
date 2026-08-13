using FluentValidation;

namespace Warehouse.Application.Features.Items.Queries.GetAllItems
{
    public class GetAllItemsQueryValidator : AbstractValidator<GetAllItemsQuery>
    {
        public GetAllItemsQueryValidator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1);

            // Capped at 100 — the same reasoning GetRecentNotificationsQuery's
            // own InclusiveBetween(1,100) already established: large enough
            // for the Angular "parent item" picker's own single unpaged-ish
            // request (page size 100) to cover a realistic catalog, small
            // enough that nobody can request the whole table in one page
            // just by passing a huge number.
            RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
        }
    }
}

using FluentValidation;

namespace Identity.Application.Features.Users.Queries.GetUsers
{
    public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
    {
        public GetUsersQueryValidator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1);

            // Same 1-100 cap Warehouse's GetAllItemsQueryValidator already
            // established — a user list is never going to be huge in this
            // system, but the cap is about not letting a single request
            // pull an unbounded table, not about this system's actual size.
            RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
        }
    }
}

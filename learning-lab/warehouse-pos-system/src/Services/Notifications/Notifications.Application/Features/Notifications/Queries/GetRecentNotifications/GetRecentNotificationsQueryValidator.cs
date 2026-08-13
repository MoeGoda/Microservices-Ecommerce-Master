using FluentValidation;

namespace Notifications.Application.Features.Notifications.Queries.GetRecentNotifications
{
    public class GetRecentNotificationsQueryValidator : AbstractValidator<GetRecentNotificationsQuery>
    {
        public GetRecentNotificationsQueryValidator()
        {
            RuleFor(q => q.Take).InclusiveBetween(1, 100);
        }
    }
}

using FluentValidation;

namespace Notifications.Application.Features.Notifications.Commands.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
    {
        public MarkNotificationAsReadCommandValidator()
        {
            RuleFor(c => c.Id).GreaterThan(0);
        }
    }
}

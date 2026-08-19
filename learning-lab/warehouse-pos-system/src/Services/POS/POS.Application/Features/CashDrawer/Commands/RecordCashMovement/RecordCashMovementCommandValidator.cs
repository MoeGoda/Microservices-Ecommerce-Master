using FluentValidation;

namespace POS.Application.Features.CashDrawer.Commands.RecordCashMovement
{
    public class RecordCashMovementCommandValidator : AbstractValidator<RecordCashMovementCommand>
    {
        public RecordCashMovementCommandValidator()
        {
            RuleFor(c => c.LocationId).GreaterThan(0);
            RuleFor(c => c.Type).IsInEnum();
            RuleFor(c => c.Amount).GreaterThan(0);
            RuleFor(c => c.Reason).NotEmpty().MaximumLength(200);
            RuleFor(c => c.CreatedByUserId).GreaterThan(0);
        }
    }
}

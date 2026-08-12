using FluentValidation;

namespace Warehouse.Application.Features.Items.Commands.AddItemBarcode
{
    public class AddItemBarcodeCommandValidator : AbstractValidator<AddItemBarcodeCommand>
    {
        public AddItemBarcodeCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.Barcode).NotEmpty().MaximumLength(50);
            RuleFor(c => c.BarcodeType).IsInEnum();
        }
    }
}

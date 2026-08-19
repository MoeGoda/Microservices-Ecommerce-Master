using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Commands.SetReceiptDiscount
{
    // A whole-sale discount applied on top of every line's own total —
    // the register's "Receipt discount" field, distinct from a per-line
    // manual discount (SetLineDiscountCommand).
    public class SetReceiptDiscountCommand : IRequest<SaleDto>
    {
        public int SaleId { get; set; }
        public decimal? ManualReceiptDiscountPercent { get; set; }
    }
}

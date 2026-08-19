using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Commands.SetLineDiscount
{
    // Sets or clears (null) a line's manual discount after it's already
    // on the sale — the register's per-line "Discount %" cell. Unlike
    // AddSaleLineCommand's ManualDiscountPercent (set once, at scan time),
    // this lets a cashier revise it afterwards.
    public class SetLineDiscountCommand : IRequest<SaleDto>
    {
        public int SaleId { get; set; }
        public int SaleLineId { get; set; }
        public decimal? ManualDiscountPercent { get; set; }
    }
}

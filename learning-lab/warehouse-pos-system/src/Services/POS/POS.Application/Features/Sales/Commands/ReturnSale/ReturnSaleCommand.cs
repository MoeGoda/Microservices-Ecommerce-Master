using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Commands.ReturnSale
{
    // The compensating flow SaleStatus.Returned's own comment named as
    // missing: reverses a COMPLETED sale, not an in-progress basket (that's
    // CancelSaleCommand's job, and it never touched Warehouse stock in the
    // first place). Only a Completed sale can transition here.
    public class ReturnSaleCommand : IRequest<SaleDto>
    {
        public int SaleId { get; set; }
    }
}

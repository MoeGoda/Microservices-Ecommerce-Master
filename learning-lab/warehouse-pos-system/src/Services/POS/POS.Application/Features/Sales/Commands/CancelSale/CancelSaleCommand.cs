using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Commands.CancelSale
{
    // An abandoned basket, not a post-payment return — see SaleStatus.Cancelled.
    public class CancelSaleCommand : IRequest<SaleDto>
    {
        public int SaleId { get; set; }
    }
}

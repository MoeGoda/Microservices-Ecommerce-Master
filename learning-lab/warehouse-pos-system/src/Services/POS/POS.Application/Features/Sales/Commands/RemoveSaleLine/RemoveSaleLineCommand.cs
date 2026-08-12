using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Commands.RemoveSaleLine
{
    public class RemoveSaleLineCommand : IRequest<SaleDto>
    {
        public int SaleId { get; set; }
        public int SaleLineId { get; set; }
    }
}

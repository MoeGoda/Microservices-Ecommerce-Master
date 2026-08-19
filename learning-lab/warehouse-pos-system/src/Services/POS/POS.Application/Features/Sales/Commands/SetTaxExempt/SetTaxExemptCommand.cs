using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Commands.SetTaxExempt
{
    public class SetTaxExemptCommand : IRequest<SaleDto>
    {
        public int SaleId { get; set; }
        public bool IsTaxExempt { get; set; }
    }
}

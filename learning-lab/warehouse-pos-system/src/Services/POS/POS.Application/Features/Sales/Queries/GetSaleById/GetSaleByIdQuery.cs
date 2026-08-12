using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Queries.GetSaleById
{
    public class GetSaleByIdQuery : IRequest<SaleDto>
    {
        public int Id { get; set; }
    }
}

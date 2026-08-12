using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Queries.GetSaleById
{
    public class GetSaleByIdQueryHandler : IRequestHandler<GetSaleByIdQuery, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;

        public GetSaleByIdQueryHandler(ISaleRepository saleRepository, ISaleLineRepository saleLineRepository)
        {
            _saleRepository = saleRepository;
            _saleLineRepository = saleLineRepository;
        }

        public async Task<SaleDto> Handle(GetSaleByIdQuery request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetById(request.Id)
                ?? throw new NotFoundException(nameof(Sale), request.Id);

            var lines = await _saleLineRepository.GetBySale(sale.Id);
            return SaleDto.FromEntity(sale, lines);
        }
    }
}

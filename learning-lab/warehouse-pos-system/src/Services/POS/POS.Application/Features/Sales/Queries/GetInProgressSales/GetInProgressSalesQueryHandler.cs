using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Queries.GetInProgressSales
{
    public class GetInProgressSalesQueryHandler : IRequestHandler<GetInProgressSalesQuery, IEnumerable<SaleDto>>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;

        public GetInProgressSalesQueryHandler(ISaleRepository saleRepository, ISaleLineRepository saleLineRepository)
        {
            _saleRepository = saleRepository;
            _saleLineRepository = saleLineRepository;
        }

        public async Task<IEnumerable<SaleDto>> Handle(GetInProgressSalesQuery request, CancellationToken cancellationToken)
        {
            var sales = await _saleRepository.GetInProgress(request.LocationId);

            var dtos = new List<SaleDto>();
            foreach (var sale in sales)
            {
                var lines = await _saleLineRepository.GetBySale(sale.Id);
                dtos.Add(SaleDto.FromEntity(sale, lines));
            }

            return dtos;
        }
    }
}

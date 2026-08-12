using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.StartSale
{
    public class StartSaleCommandHandler : IRequestHandler<StartSaleCommand, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StartSaleCommandHandler(ISaleRepository saleRepository, IUnitOfWork unitOfWork)
        {
            _saleRepository = saleRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SaleDto> Handle(StartSaleCommand request, CancellationToken cancellationToken)
        {
            var sale = new Sale
            {
                LocationId = request.LocationId,
                CashierUserId = request.CashierUserId,
                Status = SaleStatus.InProgress,
                Total = 0,
            };

            await _saleRepository.AddAsync(sale);
            await _unitOfWork.SaveChangesAsync();

            return SaleDto.FromEntity(sale, Enumerable.Empty<SaleLine>());
        }
    }
}

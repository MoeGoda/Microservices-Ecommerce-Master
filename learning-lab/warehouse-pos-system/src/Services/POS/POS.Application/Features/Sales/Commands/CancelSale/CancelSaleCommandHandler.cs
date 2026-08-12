using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.CancelSale
{
    public class CancelSaleCommandHandler : IRequestHandler<CancelSaleCommand, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CancelSaleCommandHandler(
            ISaleRepository saleRepository,
            ISaleLineRepository saleLineRepository,
            IUnitOfWork unitOfWork)
        {
            _saleRepository = saleRepository;
            _saleLineRepository = saleLineRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SaleDto> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetById(request.SaleId)
                ?? throw new NotFoundException(nameof(Sale), request.SaleId);

            if (sale.Status != SaleStatus.InProgress)
            {
                throw new ConflictException($"Sale {sale.Id} is {sale.Status}; only an InProgress sale can be cancelled.");
            }

            sale.Status = SaleStatus.Cancelled;
            await _saleRepository.UpdateAsync(sale);
            await _unitOfWork.SaveChangesAsync();

            var lines = await _saleLineRepository.GetBySale(sale.Id);
            return SaleDto.FromEntity(sale, lines);
        }
    }
}

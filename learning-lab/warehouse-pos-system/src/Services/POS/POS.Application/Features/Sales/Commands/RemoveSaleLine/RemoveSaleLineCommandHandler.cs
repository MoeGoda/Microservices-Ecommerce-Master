using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.RemoveSaleLine
{
    public class RemoveSaleLineCommandHandler : IRequestHandler<RemoveSaleLineCommand, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveSaleLineCommandHandler(
            ISaleRepository saleRepository,
            ISaleLineRepository saleLineRepository,
            IUnitOfWork unitOfWork)
        {
            _saleRepository = saleRepository;
            _saleLineRepository = saleLineRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SaleDto> Handle(RemoveSaleLineCommand request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetById(request.SaleId)
                ?? throw new NotFoundException(nameof(Sale), request.SaleId);

            if (sale.Status != SaleStatus.InProgress)
            {
                throw new ConflictException($"Sale {sale.Id} is {sale.Status}; lines can only be removed from a sale that is still InProgress.");
            }

            var line = await _saleLineRepository.GetById(request.SaleLineId);
            if (line is null || line.SaleId != sale.Id)
            {
                throw new NotFoundException(nameof(SaleLine), request.SaleLineId);
            }

            sale.Total -= line.LineTotal;
            await _saleLineRepository.DeleteAsync(line);
            await _saleRepository.UpdateAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            var lines = await _saleLineRepository.GetBySale(sale.Id);
            return SaleDto.FromEntity(sale, lines);
        }
    }
}

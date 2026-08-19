using Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Options;
using POS.Application.Common;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.Sales;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.RemoveSaleLine
{
    public class RemoveSaleLineCommandHandler : IRequestHandler<RemoveSaleLineCommand, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TaxSettings _taxSettings;

        public RemoveSaleLineCommandHandler(
            ISaleRepository saleRepository,
            ISaleLineRepository saleLineRepository,
            IUnitOfWork unitOfWork,
            IOptions<TaxSettings> taxSettings)
        {
            _saleRepository = saleRepository;
            _saleLineRepository = saleLineRepository;
            _unitOfWork = unitOfWork;
            _taxSettings = taxSettings.Value;
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

            await _saleLineRepository.DeleteAsync(line);

            var remainingLines = (await _saleLineRepository.GetBySale(sale.Id)).Where(l => l.Id != line.Id);
            SaleTotalsCalculator.Recompute(sale, remainingLines, _taxSettings.RatePercent);
            await _saleRepository.UpdateAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            return SaleDto.FromEntity(sale, await _saleLineRepository.GetBySale(sale.Id));
        }
    }
}

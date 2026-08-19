using Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Options;
using POS.Application.Common;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.Sales;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.SetLineDiscount
{
    public class SetLineDiscountCommandHandler : IRequestHandler<SetLineDiscountCommand, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TaxSettings _taxSettings;

        public SetLineDiscountCommandHandler(
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

        public async Task<SaleDto> Handle(SetLineDiscountCommand request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetById(request.SaleId)
                ?? throw new NotFoundException(nameof(Sale), request.SaleId);

            if (sale.Status != SaleStatus.InProgress)
            {
                throw new ConflictException($"Sale {sale.Id} is {sale.Status}; a line discount can only be changed on a sale that is still InProgress.");
            }

            var line = await _saleLineRepository.GetById(request.SaleLineId);
            if (line is null || line.SaleId != sale.Id)
            {
                throw new NotFoundException(nameof(SaleLine), request.SaleLineId);
            }

            // Same mutual-exclusivity rule AddSaleLineCommandHandler
            // applies at scan time: a line that already carries an
            // automatic promotion can't also take a manual discount.
            if (request.ManualDiscountPercent.HasValue && line.PromotionId.HasValue)
            {
                throw new ConflictException($"Sale line {line.Id} already has an automatic promotion applied; a manual discount can't also be set.");
            }

            line.ManualDiscountPercent = request.ManualDiscountPercent;
            line.LineTotal = Math.Round(line.UnitPrice * line.Quantity * (1 - (request.ManualDiscountPercent ?? 0) / 100m), 2);

            var lines = await _saleLineRepository.GetBySale(sale.Id);
            SaleTotalsCalculator.Recompute(sale, lines, _taxSettings.RatePercent);
            await _saleRepository.UpdateAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            return SaleDto.FromEntity(sale, lines);
        }
    }
}

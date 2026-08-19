using Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Options;
using POS.Application.Common;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.Sales;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.SetTaxExempt
{
    public class SetTaxExemptCommandHandler : IRequestHandler<SetTaxExemptCommand, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TaxSettings _taxSettings;

        public SetTaxExemptCommandHandler(
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

        public async Task<SaleDto> Handle(SetTaxExemptCommand request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetById(request.SaleId)
                ?? throw new NotFoundException(nameof(Sale), request.SaleId);

            if (sale.Status != SaleStatus.InProgress)
            {
                throw new ConflictException($"Sale {sale.Id} is {sale.Status}; tax-exempt status can only be changed on a sale that is still InProgress.");
            }

            sale.IsTaxExempt = request.IsTaxExempt;

            var lines = await _saleLineRepository.GetBySale(sale.Id);
            SaleTotalsCalculator.Recompute(sale, lines, _taxSettings.RatePercent);
            await _saleRepository.UpdateAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            return SaleDto.FromEntity(sale, lines);
        }
    }
}

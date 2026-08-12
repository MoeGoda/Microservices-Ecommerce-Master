using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.Checkout
{
    public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CheckoutCommandHandler(
            ISaleRepository saleRepository,
            ISaleLineRepository saleLineRepository,
            IUnitOfWork unitOfWork)
        {
            _saleRepository = saleRepository;
            _saleLineRepository = saleLineRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SaleDto> Handle(CheckoutCommand request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetById(request.SaleId)
                ?? throw new NotFoundException(nameof(Sale), request.SaleId);

            if (sale.Status != SaleStatus.InProgress)
            {
                throw new ConflictException($"Sale {sale.Id} is {sale.Status}; only an InProgress sale can be checked out.");
            }

            var lines = (await _saleLineRepository.GetBySale(sale.Id)).ToList();
            if (lines.Count == 0)
            {
                throw new ConflictException($"Sale {sale.Id} has no lines; add at least one before checking out.");
            }

            sale.Status = SaleStatus.Completed;
            sale.CompletedAt = DateTime.UtcNow;
            await _saleRepository.UpdateAsync(sale);

            // Step C3's SaleCompleted event fires from right here, once
            // that machinery exists — this commit is deliberately the
            // full extent of what checkout does today: finalize the sale
            // in POS's own database, nothing more.
            await _unitOfWork.SaveChangesAsync();

            return SaleDto.FromEntity(sale, lines);
        }
    }
}

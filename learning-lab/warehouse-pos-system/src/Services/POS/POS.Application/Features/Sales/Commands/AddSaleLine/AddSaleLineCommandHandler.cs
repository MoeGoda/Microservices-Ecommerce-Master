using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.AddSaleLine
{
    public class AddSaleLineCommandHandler : IRequestHandler<AddSaleLineCommand, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddSaleLineCommandHandler(
            ISaleRepository saleRepository,
            ISaleLineRepository saleLineRepository,
            IUnitOfWork unitOfWork)
        {
            _saleRepository = saleRepository;
            _saleLineRepository = saleLineRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SaleDto> Handle(AddSaleLineCommand request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetById(request.SaleId)
                ?? throw new NotFoundException(nameof(Sale), request.SaleId);

            if (sale.Status != SaleStatus.InProgress)
            {
                throw new ConflictException($"Sale {sale.Id} is {sale.Status}; lines can only be added to a sale that is still InProgress.");
            }

            var lineTotal = request.UnitPrice * request.Quantity;

            var line = new SaleLine
            {
                SaleId = sale.Id,
                Sale = sale,
                ItemId = request.ItemId,
                Sku = request.Sku,
                ItemName = request.ItemName,
                UnitPrice = request.UnitPrice,
                Quantity = request.Quantity,
                LineTotal = lineTotal,
            };
            await _saleLineRepository.AddAsync(line);

            sale.Total += lineTotal;
            await _saleRepository.UpdateAsync(sale);

            // The new line and the updated running Total commit in the same
            // call — see IUnitOfWork. A crash between the two would
            // otherwise leave Sale.Total not matching the sum of its lines.
            await _unitOfWork.SaveChangesAsync();

            var lines = await _saleLineRepository.GetBySale(sale.Id);
            return SaleDto.FromEntity(sale, lines);
        }
    }
}

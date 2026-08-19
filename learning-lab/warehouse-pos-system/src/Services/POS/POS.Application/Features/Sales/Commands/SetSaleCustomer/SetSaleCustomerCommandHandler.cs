using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.SetSaleCustomer
{
    public class SetSaleCustomerCommandHandler : IRequestHandler<SetSaleCustomerCommand, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SetSaleCustomerCommandHandler(
            ISaleRepository saleRepository,
            ISaleLineRepository saleLineRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork)
        {
            _saleRepository = saleRepository;
            _saleLineRepository = saleLineRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SaleDto> Handle(SetSaleCustomerCommand request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetById(request.SaleId)
                ?? throw new NotFoundException(nameof(Sale), request.SaleId);

            if (sale.Status != SaleStatus.InProgress)
            {
                throw new ConflictException($"Sale {sale.Id} is {sale.Status}; a customer can only be attached to a sale that is still InProgress.");
            }

            if (request.CustomerId.HasValue)
            {
                _ = await _customerRepository.GetById(request.CustomerId.Value)
                    ?? throw new NotFoundException(nameof(Customer), request.CustomerId.Value);
            }

            sale.CustomerId = request.CustomerId;
            await _saleRepository.UpdateAsync(sale);
            await _unitOfWork.SaveChangesAsync();

            var lines = await _saleLineRepository.GetBySale(sale.Id);
            return SaleDto.FromEntity(sale, lines);
        }
    }
}

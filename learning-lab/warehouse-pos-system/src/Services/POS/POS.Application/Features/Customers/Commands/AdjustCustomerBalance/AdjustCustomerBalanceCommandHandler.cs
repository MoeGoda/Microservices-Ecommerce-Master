using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Customers.Commands.AdjustCustomerBalance
{
    public class AdjustCustomerBalanceCommandHandler : IRequestHandler<AdjustCustomerBalanceCommand, CustomerDto>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AdjustCustomerBalanceCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
        {
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CustomerDto> Handle(AdjustCustomerBalanceCommand request, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.GetById(request.CustomerId)
                ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

            customer.Balance += request.Delta;

            await _customerRepository.UpdateAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            return CustomerDto.FromEntity(customer);
        }
    }
}

using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Customers.Commands.UpdateCustomer
{
    public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
        {
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.GetById(request.Id)
                ?? throw new NotFoundException(nameof(Customer), request.Id);

            customer.Name = request.Name;
            customer.Phone = request.Phone;
            customer.Email = request.Email;

            await _customerRepository.UpdateAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            return CustomerDto.FromEntity(customer);
        }
    }
}

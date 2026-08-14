using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Suppliers.Commands.SetSupplierActive
{
    public class SetSupplierActiveCommandHandler : IRequestHandler<SetSupplierActiveCommand, SupplierDto>
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SetSupplierActiveCommandHandler(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
        {
            _supplierRepository = supplierRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SupplierDto> Handle(SetSupplierActiveCommand request, CancellationToken cancellationToken)
        {
            var supplier = await _supplierRepository.GetById(request.SupplierId)
                ?? throw new NotFoundException(nameof(Supplier), request.SupplierId);

            supplier.IsActive = request.IsActive;
            await _supplierRepository.UpdateAsync(supplier);
            await _unitOfWork.SaveChangesAsync();

            return SupplierDto.FromEntity(supplier);
        }
    }
}

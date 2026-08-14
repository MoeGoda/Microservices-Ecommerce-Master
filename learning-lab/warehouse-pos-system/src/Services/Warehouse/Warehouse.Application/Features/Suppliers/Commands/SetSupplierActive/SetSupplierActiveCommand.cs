using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Suppliers.Commands.SetSupplierActive
{
    public class SetSupplierActiveCommand : IRequest<SupplierDto>
    {
        public int SupplierId { get; set; }
        public bool IsActive { get; set; }
    }
}

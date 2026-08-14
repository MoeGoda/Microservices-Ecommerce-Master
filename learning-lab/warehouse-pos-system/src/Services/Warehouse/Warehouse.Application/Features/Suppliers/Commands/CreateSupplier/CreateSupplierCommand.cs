using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Suppliers.Commands.CreateSupplier
{
    public class CreateSupplierCommand : IRequest<SupplierDto>
    {
        public string Name { get; set; } = null!;
        public string? ContactName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}

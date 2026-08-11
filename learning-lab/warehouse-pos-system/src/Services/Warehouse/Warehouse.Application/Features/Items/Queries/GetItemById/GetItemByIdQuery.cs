using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.GetItemById
{
    public class GetItemByIdQuery : IRequest<ItemDetailDto>
    {
        public int Id { get; set; }
    }
}

using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.MasterData.Queries.GetCategories
{
    // No CreateCategoryCommand alongside this — Category is fixed
    // migration-seeded reference data (see WarehouseContext), the same
    // status Identity's Roles have. This query exists purely so an admin
    // screen can populate a dropdown.
    public class GetCategoriesQuery : IRequest<IEnumerable<CategoryDto>>
    {
    }
}

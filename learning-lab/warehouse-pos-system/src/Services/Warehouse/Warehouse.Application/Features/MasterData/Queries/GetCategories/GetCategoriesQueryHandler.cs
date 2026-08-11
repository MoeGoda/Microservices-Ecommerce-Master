using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.MasterData.Queries.GetCategories
{
    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
    {
        private readonly ICategoryRepository _categoryRepository;

        public GetCategoriesQueryHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await _categoryRepository.GetAll();
            return categories.Select(CategoryDto.FromEntity);
        }
    }
}

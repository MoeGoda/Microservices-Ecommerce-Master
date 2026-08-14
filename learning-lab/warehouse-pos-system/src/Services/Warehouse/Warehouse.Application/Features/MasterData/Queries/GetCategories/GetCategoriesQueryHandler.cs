using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.MasterData;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.MasterData.Queries.GetCategories
{
    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
    {
        private const string CacheKey = "warehouse:master-data:categories";

        private readonly ICategoryRepository _categoryRepository;
        private readonly MasterDataCache _cache;

        public GetCategoriesQueryHandler(ICategoryRepository categoryRepository, MasterDataCache cache)
        {
            _categoryRepository = categoryRepository;
            _cache = cache;
        }

        public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            return await _cache.GetOrSetAsync(CacheKey, async () =>
            {
                var categories = await _categoryRepository.GetAll();
                return categories.Select(CategoryDto.FromEntity);
            }, cancellationToken);
        }
    }
}

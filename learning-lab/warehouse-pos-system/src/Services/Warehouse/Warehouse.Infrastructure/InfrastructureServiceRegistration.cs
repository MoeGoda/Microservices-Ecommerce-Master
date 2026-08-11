using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Infrastructure.Persistence;
using Warehouse.Infrastructure.Repositories;

namespace Warehouse.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<WarehouseContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("WarehouseConnectionString")));

            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<IItemBarcodeRepository, ItemBarcodeRepository>();
            services.AddScoped<IItemUnitRepository, ItemUnitRepository>();
            services.AddScoped<IStockLevelRepository, StockLevelRepository>();
            services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();

            return services;
        }
    }
}

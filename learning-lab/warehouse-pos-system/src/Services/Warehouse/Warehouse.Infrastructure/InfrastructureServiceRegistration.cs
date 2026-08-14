using Common.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Warehouse.Application.Contracts.Infrastructure;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Infrastructure.Http;
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

            // F1 — the concrete IDistributedCache implementation behind
            // MasterDataCache (Warehouse.Application). A plain connection
            // string, same idiom as WarehouseConnectionString above,
            // rather than a dedicated RedisSettings options class: there's
            // nothing else to configure per-instance beyond where Redis
            // lives.
            services.AddStackExchangeRedisCache(options =>
                options.Configuration = configuration.GetConnectionString("Redis"));

            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<IItemBarcodeRepository, ItemBarcodeRepository>();
            services.AddScoped<IItemUnitRepository, ItemUnitRepository>();
            services.AddScoped<IStockLevelRepository, StockLevelRepository>();
            services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();
            services.AddScoped<IProcessedSaleEventRepository, ProcessedSaleEventRepository>();
            services.AddScoped<IProcessedSaleReturnEventRepository, ProcessedSaleReturnEventRepository>();
            services.AddScoped<IItemPriceHistoryRepository, ItemPriceHistoryRepository>();
            services.AddScoped<IPromotionRepository, PromotionRepository>();
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Warehouse's first outbound service-to-service call (D1) —
            // same JwtSettings section every service binds, same
            // ServiceAuthHandler idiom POS.Infrastructure already uses
            // for its own outbound calls (C2/C3).
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<ReportingApiOptions>(configuration.GetSection("ReportingApi"));
            services.Configure<NotificationsApiOptions>(configuration.GetSection("NotificationsApi"));

            services.AddTransient<ServiceAuthHandler>();
            services.AddHttpClient<ReportingEventPublisher>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<ReportingApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddHttpMessageHandler<ServiceAuthHandler>();
            services.AddScoped<IEventPublisher>(sp => sp.GetRequiredService<ReportingEventPublisher>());

            // Warehouse's second-ever outbound consumer (E1) — same
            // typed-HttpClient-plus-ServiceAuthHandler shape as
            // ReportingEventPublisher above.
            services.AddHttpClient<NotificationsEventPublisher>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<NotificationsApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddHttpMessageHandler<ServiceAuthHandler>();
            services.AddScoped<IEventPublisher>(sp => sp.GetRequiredService<NotificationsEventPublisher>());

            return services;
        }
    }
}

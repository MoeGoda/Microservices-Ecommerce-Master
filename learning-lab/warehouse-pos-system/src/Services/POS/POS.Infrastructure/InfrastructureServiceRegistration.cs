using Common.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Contracts.Persistence;
using POS.Infrastructure.Http;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Repositories;

namespace POS.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PosContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("PosConnectionString")));

            services.AddScoped<ISaleRepository, SaleRepository>();
            services.AddScoped<ISaleLineRepository, SaleLineRepository>();
            services.AddScoped<ISaleCompletedOutboxRepository, SaleCompletedOutboxRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Same JwtSettings section every service binds (Common.Security)
            // — ServiceAuthHandler needs Secret/Issuer/Audience to mint a
            // token Warehouse.API's JwtBearer validation will accept.
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<WarehouseApiOptions>(configuration.GetSection("WarehouseApi"));

            services.AddTransient<ServiceAuthHandler>();
            services.AddHttpClient<IWarehouseCatalogClient, WarehouseCatalogClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<WarehouseApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddHttpMessageHandler<ServiceAuthHandler>();

            // Same pattern, same target service, different endpoint — see
            // WarehouseEventPublisher.
            services.AddHttpClient<ISaleCompletedPublisher, WarehouseEventPublisher>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<WarehouseApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddHttpMessageHandler<ServiceAuthHandler>();

            // SaleCompletedOutboxBackgroundService is deliberately NOT
            // registered via AddHostedService here — see its own comment.
            // There's no POS.API host to run it in yet.

            return services;
        }
    }
}

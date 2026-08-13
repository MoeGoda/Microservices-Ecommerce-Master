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
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Same JwtSettings section every service binds (Common.Security)
            // — ServiceAuthHandler needs Secret/Issuer/Audience to mint a
            // token Warehouse.API's/Reporting.API's JwtBearer validation
            // will accept.
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<WarehouseApiOptions>(configuration.GetSection("WarehouseApi"));
            services.Configure<ReportingApiOptions>(configuration.GetSection("ReportingApi"));
            services.Configure<NotificationsApiOptions>(configuration.GetSection("NotificationsApi"));

            services.AddTransient<ServiceAuthHandler>();
            services.AddHttpClient<IWarehouseCatalogClient, WarehouseCatalogClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<WarehouseApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddHttpMessageHandler<ServiceAuthHandler>();

            // Three IEventPublisher implementations now (D1, E1) — one per
            // consumer a SaleCompleted event fans out to. Each is
            // registered as itself via AddHttpClient<T>() (so it gets its
            // own configured HttpClient) and then re-exposed under
            // IEventPublisher by resolving that same instance, rather
            // than registering the interface directly — AddHttpClient's
            // typed-client sugar only works against a concrete type.
            // OutboxDispatcher resolves IEnumerable<IEventPublisher> and
            // picks the one whose ConsumerName matches a given delivery.
            services.AddHttpClient<WarehouseEventPublisher>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<WarehouseApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddHttpMessageHandler<ServiceAuthHandler>();
            services.AddScoped<IEventPublisher>(sp => sp.GetRequiredService<WarehouseEventPublisher>());

            services.AddHttpClient<ReportingEventPublisher>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<ReportingApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddHttpMessageHandler<ServiceAuthHandler>();
            services.AddScoped<IEventPublisher>(sp => sp.GetRequiredService<ReportingEventPublisher>());

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

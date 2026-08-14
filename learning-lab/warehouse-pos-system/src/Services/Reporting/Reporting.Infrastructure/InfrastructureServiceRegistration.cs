using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reporting.Application.Contracts.Persistence;
using Reporting.Infrastructure.Persistence;
using Reporting.Infrastructure.Repositories;

namespace Reporting.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ReportingContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("ReportingConnectionString")));

            services.AddScoped<ISaleRecordRepository, SaleRecordRepository>();
            services.AddScoped<ISaleLineRecordRepository, SaleLineRecordRepository>();
            services.AddScoped<IStockLevelRecordRepository, StockLevelRecordRepository>();
            services.AddScoped<IStockMovementRecordRepository, StockMovementRecordRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}

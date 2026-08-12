using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Warehouse.Application.Behaviours;

namespace Warehouse.Application
{
    // Same shape as Identity.Application's registration (A1) — one
    // extension method Warehouse.API (B3) calls, so its startup code never
    // needs to know MediatR or FluentValidation exist.
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly));
            services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

            return services;
        }
    }
}

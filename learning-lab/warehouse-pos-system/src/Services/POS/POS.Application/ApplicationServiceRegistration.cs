using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Behaviours;
using POS.Application.Features.Outbox;

namespace POS.Application
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly));
            services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

            // Not a MediatR handler — nothing sends it a request. Driven
            // by a poll loop (OutboxBackgroundService, POS.Infrastructure).
            services.AddScoped<OutboxDispatcher>();

            return services;
        }
    }
}

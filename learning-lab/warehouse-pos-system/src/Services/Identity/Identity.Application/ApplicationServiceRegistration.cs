using FluentValidation;
using Identity.Application.Behaviours;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application
{
    // One extension method Identity.API calls in Program.cs, so
    // ConfigureServices doesn't need to know MediatR/FluentValidation exist —
    // it just says "give me the Application layer's services."
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly));
            services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);

            // Order matters: validation runs before the unhandled-exception
            // catch-all, so a ValidationException is thrown *inside* the
            // logging behaviour's try/catch and explicitly excluded there
            // (see UnhandledExceptionBehaviour) rather than logged as a
            // surprise 500.
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

            return services;
        }
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Common.ExceptionHandling
{
    public static class ExceptionHandlingServiceRegistration
    {
        // Call from Program.cs alongside AddApplicationServices/AddInfrastructureServices.
        public static IServiceCollection AddCommonExceptionHandling(this IServiceCollection services)
        {
            services.AddExceptionHandler<GlobalExceptionHandler>();

            // AddProblemDetails() is what makes ProblemDetails responses
            // consistent for *every* error path in the app, not just the
            // ones GlobalExceptionHandler builds — 404s from routing,
            // 400s from [ApiController]'s automatic model validation, etc.
            // all come out shaped the same way once this is registered.
            services.AddProblemDetails();

            return services;
        }

        // Must be one of the first things in the middleware pipeline — it
        // can only catch exceptions thrown by middleware registered *after*
        // it runs.
        public static IApplicationBuilder UseCommonExceptionHandling(this IApplicationBuilder app)
        {
            app.UseExceptionHandler();
            return app;
        }
    }
}

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Common.RequestCulture
{
    public static class RequestCultureServiceRegistration
    {
        // English and Arabic only, per F3's scope — matches the client's
        // own language switcher (client/src/app/core/i18n).
        public static readonly string[] SupportedCultures = ["en", "ar"];

        // Call from Program.cs alongside AddCommonExceptionHandling.
        public static IServiceCollection AddSharedRequestLocalization(this IServiceCollection services)
        {
            services.AddLocalization();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var cultures = SupportedCultures.Select(c => new CultureInfo(c)).ToList();

                // Fully qualified: this project's own namespace is also
                // named RequestCulture, which shadows Microsoft.AspNetCore
                // .Localization's RequestCulture type at this scope.
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en");
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;

                // ASP.NET Core registers QueryString, Cookie, then
                // Accept-Language providers by default, in that order — the
                // header alone is enough for both the API clients (Angular
                // sets it from the language switcher, see the http
                // interceptor) and this phase's smoke tests, so the
                // defaults are kept as-is rather than trimmed.
            });

            return services;
        }

        // Must run before UseAuthentication/MapControllers — CurrentUICulture
        // needs to be set on the request thread before any validator or
        // exception constructor runs. Exception-handler middleware itself
        // doesn't need the culture, but placing this right after it keeps
        // all the "early, cross-cutting" middleware grouped together.
        public static IApplicationBuilder UseSharedRequestLocalization(this IApplicationBuilder app)
        {
            app.UseRequestLocalization();
            return app;
        }
    }
}

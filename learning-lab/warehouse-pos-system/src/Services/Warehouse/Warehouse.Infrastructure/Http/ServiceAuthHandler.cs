using System.Net.Http.Headers;
using System.Security.Claims;
using Common.Security;
using Microsoft.Extensions.Options;

namespace Warehouse.Infrastructure.Http
{
    // Same idiom as POS.Infrastructure's own ServiceAuthHandler (C2) —
    // Warehouse's first outbound call (ReportingEventPublisher, D1) needs
    // a token Reporting.API's [Authorize] controllers will accept, and
    // there's no signed-in user in this picture: this is Warehouse *the
    // service* calling Reporting *the service*. Same shared-secret
    // tradeoff already flagged for POS's own handler — Phase F2
    // territory, not this step's.
    public class ServiceAuthHandler : DelegatingHandler
    {
        private readonly JwtSettings _jwtSettings;

        public ServiceAuthHandler(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = JwtTokenFactory.CreateToken(
                _jwtSettings,
                new[] { new Claim(ClaimTypes.Name, "warehouse-service") },
                DateTime.UtcNow.AddMinutes(5));

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return base.SendAsync(request, cancellationToken);
        }
    }
}

using System.Net.Http.Headers;
using System.Security.Claims;
using Common.Security;
using Microsoft.Extensions.Options;

namespace POS.Infrastructure.Http
{
    // Attaches a Bearer token to every outgoing call to Warehouse.API —
    // Warehouse's controllers are [Authorize] on every route (B3), so
    // this call needs a valid one just like a signed-in user's request
    // would. There's no signed-in user in this picture, though: this is
    // POS *the service* calling Warehouse *the service*, which is why the
    // token represents "pos-service" rather than forwarding anyone's
    // actual session token (POS.API doesn't exist yet to have one to
    // forward, and even once it does, C2 is specifically the sync call
    // POS's own backend makes, not a pass-through of the cashier's token).
    //
    // Signing this with the SAME shared secret JwtSettings:Secret every
    // service already reads (Common.Security) is the simplest form of
    // service-to-service auth that works without adding anything new —
    // no separate service-account database, no OAuth2 client-credentials
    // flow. The honest tradeoff: every service that can read this secret
    // can mint a token claiming to be any service, including "pos-service."
    // A real deployment would want a dedicated token-issuing endpoint
    // (Identity.API, most naturally) so only ONE place ever signs a
    // token — that's Phase F2 (security hardening) territory, not this step.
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
                new[] { new Claim(ClaimTypes.Name, "pos-service") },
                DateTime.UtcNow.AddMinutes(5));

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return base.SendAsync(request, cancellationToken);
        }
    }
}

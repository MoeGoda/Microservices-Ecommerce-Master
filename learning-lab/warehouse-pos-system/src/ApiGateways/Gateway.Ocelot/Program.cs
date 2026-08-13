using System.Threading.RateLimiting;
using Common.ExceptionHandling;
using Common.Security;
using Microsoft.AspNetCore.RateLimiting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Ocelot's own config, on top of appsettings.json — reloadOnChange means
// editing ocelot.json while the gateway is running (adding a route, say)
// takes effect without a restart.
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// The single shared JwtSettings source — see Identity.API's Program.cs
// for the full reasoning. Editing SharedSettings/jwt.settings.json is now
// the only place this value ever needs to change; AddJwtAuthentication
// below still just reads IConfiguration's "JwtSettings" section.
builder.Configuration.AddJsonFile(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "SharedSettings", "jwt.settings.json"), optional: false, reloadOnChange: true);

// Registers the SAME "Bearer" scheme (name matters — ocelot.json's routes
// reference it by that exact string in AuthenticationOptions:AuthenticationProviderKey)
// that Identity.API uses to validate tokens. This is the crux of A3: the
// gateway now does its own JWT validation *before* proxying — a request
// with no token, an expired token, or a token signed with the wrong secret
// never reaches Identity.API at all.
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddCommonExceptionHandling();
builder.Services.AddHealthChecks();

// Ocelot ships its own RateLimitOptions, but it identifies a "client" via a
// self-declared request header — an attacker credential-stuffing the login
// route just omits the header and gets an unlimited budget. That's a real
// finding from testing this route, not a hypothetical: it initially used
// Ocelot's RateLimitOptions and every request returned 503 because no
// client understood it needed to send that header at all. A global
// ASP.NET Core RateLimiter, partitioned by the caller's IP, is the correct
// mechanism here — an attacker can't opt out of having an IP address.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var isLoginRoute = HttpMethods.IsPost(httpContext.Request.Method)
            && httpContext.Request.Path.Equals("/Identity/Auth/login", StringComparison.OrdinalIgnoreCase);

        // Every other route gets an effectively-unlimited partition — this
        // limiter exists for the login route specifically, not as a
        // general-purpose throttle on the whole gateway.
        if (!isLoginRoute)
        {
            return RateLimitPartition.GetNoLimiter("unrestricted");
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(30)
        });
    });
});

// Ocelot reads its own routing config (already loaded above) and builds
// the reverse-proxy pipeline from it.
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCommonExceptionHandling();

// Ocelot's own middleware is a catch-all: once a request reaches it, it
// either matches a configured route or Ocelot itself writes a 404 — it
// never calls next() to let anything *later* in the pipeline see the
// request. Minimal hosting normally defers Map*() calls to run implicitly
// at the very end of the pipeline, which would put /hc *after* Ocelot and
// it would never be reached (this actually happened — see the A3 writeup).
// Calling UseRouting()/UseEndpoints() explicitly, before await UseOcelot(),
// fixes the order: endpoint routing gets first look at every request, and
// only calls next() (falling through to Ocelot) when nothing matches.
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
// The ASP0014 analyzer suggests a top-level app.MapHealthChecks(...) call
// instead — which is exactly what caused the original bug: a top-level Map
// call is deferred to run implicitly at the end of the pipeline, after
// UseOcelot(). The explicit UseEndpoints() call here is what makes /hc
// dispatch at THIS point, before Ocelot gets the request.
#pragma warning disable ASP0014
app.UseEndpoints(endpoints => endpoints.MapHealthChecks("/hc"));
#pragma warning restore ASP0014

// UseOcelot() is async and is effectively the last middleware in the
// pipeline — it matches the incoming request against ocelot.json's routes,
// runs the per-route authentication/rate-limiting it's configured with,
// and proxies to the downstream service.
await app.UseOcelot();

app.Run();

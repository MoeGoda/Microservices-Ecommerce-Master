using Common.ExceptionHandling;
using Common.Security;
using Identity.Application;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// The one physical copy of JwtSettings:Secret/Issuer/Audience every
// service and the gateway used to duplicate by hand in their own
// appsettings.json — A3's own README flagged that copy-paste as a real
// gap. Editing SharedSettings/jwt.settings.json is now the only place
// that value ever needs to change; nothing below this line changed to
// pick it up, since AddJwtAuthentication still just reads
// IConfiguration's "JwtSettings" section, wherever it came from.
builder.Configuration.AddJsonFile(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "SharedSettings", "jwt.settings.json"), optional: false, reloadOnChange: true);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddCommonExceptionHandling();

// This is the piece that turns "a string in the Authorization header" into
// a populated User.Claims on every controller. The same shared extension
// (Common.Security) is what Gateway.Ocelot calls too — same
// TokenValidationParameters, same JwtSettings:Secret/Issuer/Audience, so a
// token accepted at the gateway is accepted here identically.
builder.Services.AddJwtAuthentication(builder.Configuration);

// F1 — a real dependency check, not a bare liveness probe: AddDbContextCheck
// actually opens a connection and runs a trivial query against
// IdentityContext's own database, so /hc genuinely answers "can this
// service do its job," not just "is the process alive." The gateway's own
// /hc (A3) stays a bare liveness check by design — see this project's
// README for why aggregating downstream health INTO the gateway would be
// the wrong tradeoff.
builder.Services.AddHealthChecks().AddDbContextCheck<IdentityContext>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Identity.API", Version = "v1" });

    // Adds the padlock icon + "Bearer <token>" input in Swagger UI so you
    // can paste a token from /login and immediately call [Authorize] routes
    // from the same page, instead of switching to curl/Postman.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- Auto-migrate + seed on startup ---
// Convenient for local/dev (no separate "run migrations" step before the
// service is usable) at the cost of every instance racing to migrate on
// boot — acceptable here with a single instance, but a real production
// deployment would run migrations as a separate release step instead of
// coupling it to every app startup.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
    context.Database.Migrate();
    await IdentityContextSeed.SeedAdminUserAsync(context);
}

// First in the pipeline, on purpose: it can only catch exceptions thrown by
// middleware registered after it. Authentication/authorization/routing/
// controllers all run "inside" this, so anything any of them throws — a
// ValidationException from a MediatR handler, an unexpected NullReferenceException,
// anything — gets caught here and turned into a consistent ProblemDetails response.
app.UseCommonExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity.API v1"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Not routed through Ocelot — same "service-to-service/infra tooling, not
// a browser-facing feature" reasoning as EventsController/StockEventsController
// (D1/C3): a real deployment's orchestrator (docker-compose's own
// healthcheck directive, F4) hits this directly per-container, the same
// way it would never ask the gateway "is Identity healthy?" on Identity's
// behalf.
app.MapHealthChecks("/hc");

app.Run();

using Common.ExceptionHandling;
using Common.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using POS.Application;
using POS.Infrastructure;
using POS.Infrastructure.BackgroundServices;
using POS.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// The single shared JwtSettings source — see Identity.API's Program.cs
// for the full reasoning. Editing SharedSettings/jwt.settings.json is now
// the only place this value ever needs to change.
builder.Configuration.AddJsonFile(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "SharedSettings", "jwt.settings.json"), optional: false, reloadOnChange: true);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddCommonExceptionHandling();

// Same shared extension Identity.API/Warehouse.API call, with the same
// JwtSettings:Secret/Issuer/Audience — a token minted by Identity and
// accepted at the gateway is accepted here identically. POS never issues
// tokens to end users, only validates them, the same reasoning as
// Warehouse.API.
builder.Services.AddJwtAuthentication(builder.Configuration);

// F1 — a real DB-connectivity check, not a bare liveness probe. Same
// reasoning as Identity.API's own Program.cs.
builder.Services.AddHealthChecks().AddDbContextCheck<PosContext>();

// The actual payoff of POS.API existing at all: OutboxDispatcher (C3,
// generalized in D1) was written and exercised directly by C3's own
// runtime test, but had nowhere to run on its own poll loop until there
// was a host to register it in — WarehouseContextFactory (B1) sat in the
// exact same spot before Warehouse.API showed up.
builder.Services.AddHostedService<OutboxBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "POS.API", Version = "v1" });

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

// --- Auto-migrate on startup --- (same tradeoff as Identity.API/Warehouse.API:
// convenient for local/dev, a real deployment would run migrations as a
// separate release step instead of coupling it to every app startup). POS
// has nothing analogous to Warehouse's sample-item seed to run here.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PosContext>();
    context.Database.Migrate();
}

// First in the pipeline — see Identity.API's Program.cs for why.
app.UseCommonExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "POS.API v1"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Not routed through Ocelot — same reasoning as Identity.API's own /hc.
app.MapHealthChecks("/hc");

app.Run();

// Top-level statements compile to a Program class that's normally
// invisible — this makes it a real type WebApplicationFactory<Program>
// can target, same reasoning as Warehouse.API's Program.cs.
public partial class Program { }

using Common.ExceptionHandling;
using Common.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Warehouse.Application;
using Warehouse.Infrastructure;
using Warehouse.Infrastructure.BackgroundServices;
using Warehouse.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddCommonExceptionHandling();

// Same shared extension Identity.API and Gateway.Ocelot call, with the
// same JwtSettings:Secret/Issuer/Audience — a token minted by Identity and
// accepted at the gateway is accepted here identically. Warehouse still
// never ISSUES tokens to end users, only validates them — but D1 gave
// Warehouse.Infrastructure its own reason to reference Common.Security
// too now (ServiceAuthHandler, minting a "warehouse-service" token to
// call Reporting.API), the same dual-use POS.Infrastructure already had
// since C2/C3.
builder.Services.AddJwtAuthentication(builder.Configuration);

// The outbox dispatcher (C3's pattern, generalized in D1) gets its host
// the moment this class exists — unlike POS's own C3 outbox, which had
// to wait for POS.API to exist at all, Warehouse.API is already here.
builder.Services.AddHostedService<OutboxBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Warehouse.API", Version = "v1" });

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

// --- Auto-migrate + seed on startup --- (same tradeoff as Identity.API:
// convenient for local/dev, a real deployment would run migrations as a
// separate release step instead of coupling it to every app startup).
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WarehouseContext>();
    context.Database.Migrate();
    await WarehouseContextSeed.SeedSampleItemsAsync(context);
}

// First in the pipeline — see Identity.API's Program.cs for why.
app.UseCommonExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Warehouse.API v1"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Top-level statements compile to a Program class that's normally
// invisible — this makes it a real type WebApplicationFactory<Program>
// can target, so an integration test can host the actual pipeline
// (routing, JWT auth, MediatR, exception handling) with only its
// DbContext swapped out, rather than re-testing controllers in isolation
// with everything else stubbed.
public partial class Program { }

using Common.ExceptionHandling;
using Common.RequestCulture;
using Common.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Reporting.Application;
using Reporting.Infrastructure;
using Reporting.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// The single shared JwtSettings source — see Identity.API's Program.cs
// for the full reasoning. Editing SharedSettings/jwt.settings.json is now
// the only place this value ever needs to change.
builder.Configuration.AddJsonFile(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "SharedSettings", "jwt.settings.json"), optional: false, reloadOnChange: true);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddCommonExceptionHandling();

// F3 — En/Ar culture negotiation. See Identity.API's Program.cs for why.
builder.Services.AddSharedRequestLocalization();

// Same shared extension every other service calls, same JwtSettings
// section. Reporting never issues tokens either — only validates the
// ones POS/Warehouse's own ServiceAuthHandlers mint (D1).
builder.Services.AddJwtAuthentication(builder.Configuration);

// F1 — a real DB-connectivity check, not a bare liveness probe. Same
// reasoning as Identity.API's own Program.cs.
builder.Services.AddHealthChecks().AddDbContextCheck<ReportingContext>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Reporting.API", Version = "v1" });

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

// --- Auto-migrate on startup --- (same tradeoff as every other service's
// Program.cs). Reporting has no sample data of its own to seed — every
// row it will ever have arrives via an ingested event, not a startup seed.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ReportingContext>();
    context.Database.Migrate();
}

// First in the pipeline — see Identity.API's Program.cs for why.
app.UseCommonExceptionHandling();
app.UseSharedRequestLocalization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Reporting.API v1"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Not routed through Ocelot — same reasoning as Identity.API's own /hc.
app.MapHealthChecks("/hc");

app.Run();

// Top-level statements compile to a Program class that's normally
// invisible — this makes it a real type WebApplicationFactory<Program>
// (or, as this step's own verification uses, a hand-built TestServer
// calling the same registration methods) can target.
public partial class Program { }

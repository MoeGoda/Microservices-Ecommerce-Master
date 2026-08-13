using System.Text;
using Common.ExceptionHandling;
using Common.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Notifications.API.Realtime;
using Notifications.Application;
using Notifications.Application.Contracts.Infrastructure;
using Notifications.Infrastructure;
using Notifications.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddCommonExceptionHandling();

// Hand-rolled rather than a call to Common.Security's shared
// AddJwtAuthentication — every other service uses that extension
// unchanged, but this is the one service a browser connects to directly
// (bypassing the gateway; see the README for why SignalR isn't proxied
// through Ocelot here), and a WebSocket handshake can't carry an
// Authorization header. OnMessageReceived below pulls the token from the
// query string ONLY for the hub's own path — every controller's normal
// "Authorization: Bearer ..." header check is completely untouched, and
// no other service's Program.cs had to change to get this.
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings configuration section is missing.");

const string NotificationsHubPath = "/hubs/notifications";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments(NotificationsHubPath))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
    };
});

builder.Services.AddAuthorization();

builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationPusher, SignalRNotificationPusher>();

// The one CORS policy anywhere in this system (see the README's gap
// note). It exists because the Angular client connects to this hub
// DIRECTLY, not through the gateway, so the browser enforces same-origin
// rules Ocelot never had to negotiate on this project's behalf.
// AllowCredentials is required for SignalR's negotiate handshake, which
// rules out AllowAnyOrigin (the two are mutually exclusive) — the allowed
// origin is the Angular dev server's own canonical port, per the README's
// "Run it locally" instructions.
const string NotificationsCorsPolicy = "NotificationsCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(NotificationsCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Notifications.API", Version = "v1" });

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
// Program.cs). Like Reporting, Notifications has no sample data of its
// own to seed — every row it will ever have arrives via an ingested event.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
    context.Database.Migrate();
}

// First in the pipeline — see Identity.API's Program.cs for why.
app.UseCommonExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notifications.API v1"));
}

app.UseCors(NotificationsCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// Not routed through Ocelot — a direct browser connection, see the
// README's own reasoning. [Authorize] on NotificationsHub itself (not
// here) is what actually gates it.
app.MapHub<NotificationsHub>(NotificationsHubPath);

app.Run();

public partial class Program { }

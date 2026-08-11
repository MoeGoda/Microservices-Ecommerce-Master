using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Common.Security
{
    // Every service in this system that validates a JWT — the gateway
    // first, and later every downstream service too, as defense in depth —
    // needs the exact same TokenValidationParameters. Writing this out by
    // hand in each Program.cs is exactly the kind of duplication that drifts:
    // one service tweaks ClockSkew and the others don't, and now token
    // expiry behaves differently depending which service you hit. One
    // extension method, called the same way everywhere, closes that gap.
    public static class JwtAuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings configuration section is missing.");

            services.AddAuthentication(options =>
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
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

            services.AddAuthorization();

            return services;
        }
    }
}

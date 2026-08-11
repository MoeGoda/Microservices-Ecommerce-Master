namespace Identity.Infrastructure.Security
{
    // Bound from appsettings.json's "JwtSettings" section via IOptions<T>.
    // Secret must be overridden per environment (see appsettings.json note) —
    // it's the key every downstream service (and the gateway) uses to verify
    // tokens, so it has to be identical everywhere and never committed for
    // real deployments.
    public class JwtSettings
    {
        public string Secret { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int ExpiryMinutes { get; set; } = 60;
    }
}

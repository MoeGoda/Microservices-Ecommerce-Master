namespace Common.Security
{
    // Bound from appsettings.json's "JwtSettings" section via IOptions<T>.
    // Every service that issues tokens (Identity.API), validates them
    // (Gateway.Ocelot, and later every downstream service as defense in
    // depth), or both, binds the *same* section — Secret/Issuer/Audience
    // must be byte-identical across all of them for signature validation to
    // succeed. ExpiryMinutes only matters to whoever is *issuing* tokens.
    public class JwtSettings
    {
        public string Secret { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public int ExpiryMinutes { get; set; } = 60;
    }
}

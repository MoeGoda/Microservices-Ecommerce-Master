namespace POS.Application.Common
{
    // Bound from appsettings.json's "Tax" section (POS.Infrastructure's
    // InfrastructureServiceRegistration.Configure<TaxSettings>) — same
    // IOptions<T> pattern Notifications.Infrastructure's SmtpSettings
    // already uses for a plain numeric config value, not a departure
    // from precedent. One flat rate, not a multi-jurisdiction tax engine
    // — deliberately out of scope for this feature set.
    public class TaxSettings
    {
        public decimal RatePercent { get; set; } = 8.5m;
    }
}

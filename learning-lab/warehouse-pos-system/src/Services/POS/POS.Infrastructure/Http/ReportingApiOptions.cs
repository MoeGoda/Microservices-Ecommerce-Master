namespace POS.Infrastructure.Http
{
    // Bound from configuration's "ReportingApi" section — same shape as
    // WarehouseApiOptions, a direct service-to-service base URL, not
    // through the Ocelot gateway.
    public class ReportingApiOptions
    {
        public string BaseUrl { get; set; } = null!;
    }
}

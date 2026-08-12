namespace POS.Infrastructure.Http
{
    // Bound from configuration's "WarehouseApi" section. POS calls
    // Warehouse.API directly at this base URL — NOT through the Ocelot
    // gateway. The gateway (A3) exists for external/browser callers;
    // this is a service-to-service call inside the system, so it skips
    // straight to the other service the same way a service mesh or
    // internal DNS entry would in a real deployment, just hardcoded here
    // since neither exists in this learning-lab.
    public class WarehouseApiOptions
    {
        public string BaseUrl { get; set; } = null!;
    }
}

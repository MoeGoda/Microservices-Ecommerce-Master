using Common.Exceptions;

namespace POS.Application.Exceptions
{
    // The sync call to Warehouse (IWarehouseCatalogClient) is a hard
    // dependency for adding a sale line — there's no reasonable "add it
    // anyway, verify later" fallback for a POS register. If Warehouse.API
    // is unreachable or errors, that has to surface as a clear, distinct
    // failure (503 — "the service you actually needed is down") rather
    // than an ambiguous 500 that looks like a POS-side bug.
    public class WarehouseUnavailableException : Exception, IHasStatusCode
    {
        public int StatusCode => 503;

        public WarehouseUnavailableException(string reason, Exception? inner = null)
            : base($"Warehouse service is unavailable: {reason}", inner)
        {
        }
    }
}

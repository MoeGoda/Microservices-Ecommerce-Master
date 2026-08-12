namespace POS.Application.Contracts.Infrastructure
{
    // The delivery half of the outbox pattern — SaleCompletedOutboxDispatcher
    // (Application layer, the orchestration) calls this; the real
    // implementation (Infrastructure) is an HTTP POST to Warehouse.API's
    // StockEventsController, using the same ServiceAuthHandler pattern
    // Step C2 already established for IWarehouseCatalogClient.
    public interface ISaleCompletedPublisher
    {
        Task<SaleCompletedPublishResult> PublishAsync(SaleCompletedMessage message, CancellationToken cancellationToken);
    }

    public class SaleCompletedMessage
    {
        public int SaleId { get; set; }
        public int LocationId { get; set; }
        public IReadOnlyList<SaleCompletedLine> Lines { get; set; } = Array.Empty<SaleCompletedLine>();
    }

    public class SaleCompletedLine
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class SaleCompletedPublishResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        public static SaleCompletedPublishResult Ok() => new() { Success = true };
        public static SaleCompletedPublishResult Failed(string error) => new() { Success = false, Error = error };
    }
}

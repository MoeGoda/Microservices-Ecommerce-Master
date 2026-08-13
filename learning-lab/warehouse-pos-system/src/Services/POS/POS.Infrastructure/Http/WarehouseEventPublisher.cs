using System.Net.Http.Json;
using System.Text.Json;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Features.Outbox;

namespace POS.Infrastructure.Http
{
    // The real delivery mechanism behind the "Warehouse" consumer — an
    // HTTP POST to Warehouse.API's StockEventsController (Step C3), using
    // the same typed-HttpClient-plus-ServiceAuthHandler registration
    // pattern as IWarehouseCatalogClient (C2). Understands SaleCompleted
    // and SaleReturned — each routes to its own StockEventsController
    // action, applying the same lines in opposite directions.
    public class WarehouseEventPublisher : IEventPublisher
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;

        public WarehouseEventPublisher(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string ConsumerName => OutboxConsumers.Warehouse;

        public async Task<EventPublishResult> PublishAsync(string eventType, string payloadJson, CancellationToken cancellationToken)
        {
            // SaleReturned routes to Warehouse's own ApplySaleReturnCommand
            // (a compensating stock INCREASE) instead of ApplySaleCommand's
            // decrease — same request shape either way, since both only
            // ever needed ItemId/Quantity per line; the downstream PATH is
            // what tells Warehouse which direction to apply it.
            string downstreamPath;
            if (eventType == OutboxEventTypes.SaleCompleted)
            {
                downstreamPath = "api/v1/StockEvents/sale-completed";
            }
            else if (eventType == OutboxEventTypes.SaleReturned)
            {
                downstreamPath = "api/v1/StockEvents/sale-returned";
            }
            else
            {
                return EventPublishResult.Failed($"WarehouseEventPublisher doesn't understand event type '{eventType}'.");
            }

            var message = JsonSerializer.Deserialize<SaleCompletedMessage>(payloadJson, JsonOptions);
            if (message is null)
            {
                return EventPublishResult.Failed($"{eventType} payload deserialized to null.");
            }

            // Warehouse's ApplySaleCommand/ApplySaleReturnCommand only need
            // ItemId/Quantity per line to adjust stock — everything else on
            // SaleCompletedMessage (Sku/ItemName/UnitPrice/LineTotal,
            // CashierUserId, Total) exists for Reporting's benefit, not
            // this consumer's.
            var request = new
            {
                saleId = message.SaleId,
                locationId = message.LocationId,
                lines = message.Lines.Select(l => new { itemId = l.ItemId, quantity = l.Quantity }),
            };

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(downstreamPath, request, JsonOptions, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return EventPublishResult.Ok();
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return EventPublishResult.Failed($"{(int)response.StatusCode} {response.StatusCode}: {body}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return EventPublishResult.Failed($"could not reach Warehouse.API: {ex.Message}");
            }
        }
    }
}

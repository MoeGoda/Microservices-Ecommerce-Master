using System.Net.Http.Json;
using System.Text.Json;
using POS.Application.Contracts.Infrastructure;

namespace POS.Infrastructure.Http
{
    // The real delivery mechanism behind ISaleCompletedPublisher — an
    // HTTP POST to Warehouse.API's StockEventsController (Step C3), using
    // the same typed-HttpClient-plus-ServiceAuthHandler registration
    // pattern as IWarehouseCatalogClient (C2). Any failure — network,
    // timeout, a non-2xx response, including a 409 for insufficient
    // stock — comes back as a plain failed result; it's
    // SaleCompletedOutboxDispatcher's job to decide what a failure means
    // for retries, not this client's.
    public class WarehouseEventPublisher : ISaleCompletedPublisher
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;

        public WarehouseEventPublisher(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<SaleCompletedPublishResult> PublishAsync(SaleCompletedMessage message, CancellationToken cancellationToken)
        {
            var request = new
            {
                saleId = message.SaleId,
                locationId = message.LocationId,
                lines = message.Lines.Select(l => new { itemId = l.ItemId, quantity = l.Quantity }),
            };

            try
            {
                using var response = await _httpClient.PostAsJsonAsync("api/v1/StockEvents/sale-completed", request, JsonOptions, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return SaleCompletedPublishResult.Ok();
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return SaleCompletedPublishResult.Failed($"{(int)response.StatusCode} {response.StatusCode}: {body}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return SaleCompletedPublishResult.Failed($"could not reach Warehouse.API: {ex.Message}");
            }
        }
    }
}

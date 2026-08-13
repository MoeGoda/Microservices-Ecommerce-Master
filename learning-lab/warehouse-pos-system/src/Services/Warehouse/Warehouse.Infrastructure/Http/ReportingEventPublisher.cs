using System.Net.Http.Headers;
using System.Text;
using Warehouse.Application.Contracts.Infrastructure;
using Warehouse.Application.Features.Outbox;

namespace Warehouse.Infrastructure.Http
{
    // The real delivery mechanism behind Warehouse's "Reporting" consumer
    // (D1) — same shape as POS's own ReportingEventPublisher: PayloadJson
    // is already exactly the shape Reporting's IngestStockLevelChangedCommand
    // wants, so it's forwarded verbatim rather than deserialized and
    // re-serialized.
    public class ReportingEventPublisher : IEventPublisher
    {
        private readonly HttpClient _httpClient;

        public ReportingEventPublisher(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string ConsumerName => OutboxConsumers.Reporting;

        public async Task<EventPublishResult> PublishAsync(string eventType, string payloadJson, CancellationToken cancellationToken)
        {
            if (eventType != OutboxEventTypes.StockLevelChanged)
            {
                return EventPublishResult.Failed($"ReportingEventPublisher doesn't understand event type '{eventType}'.");
            }

            try
            {
                using var content = new StringContent(payloadJson, Encoding.UTF8);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                using var response = await _httpClient.PostAsync("api/v1/Events/stock-level-changed", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return EventPublishResult.Ok();
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return EventPublishResult.Failed($"{(int)response.StatusCode} {response.StatusCode}: {body}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return EventPublishResult.Failed($"could not reach Reporting.API: {ex.Message}");
            }
        }
    }
}

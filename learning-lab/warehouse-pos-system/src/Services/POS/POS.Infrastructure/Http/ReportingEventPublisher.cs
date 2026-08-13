using System.Net.Http.Headers;
using System.Text;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Features.Outbox;

namespace POS.Infrastructure.Http
{
    // The real delivery mechanism behind the "Reporting" consumer (D1) —
    // an HTTP POST to Reporting.API, same typed-HttpClient-plus-
    // ServiceAuthHandler shape as WarehouseEventPublisher. Reporting's own
    // IngestSaleCompletedCommand wants the EXACT same fields
    // SaleCompletedMessage already carries, so the outbox's own
    // PayloadJson is forwarded verbatim rather than deserialized and
    // re-serialized — there's no shape translation to do here, unlike
    // WarehouseEventPublisher's ItemId/Quantity-only projection.
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
            if (eventType != OutboxEventTypes.SaleCompleted)
            {
                return EventPublishResult.Failed($"ReportingEventPublisher doesn't understand event type '{eventType}'.");
            }

            try
            {
                using var content = new StringContent(payloadJson, Encoding.UTF8);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                using var response = await _httpClient.PostAsync("api/v1/Events/sale-completed", content, cancellationToken);

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

using System.Net.Http.Headers;
using System.Text;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Features.Outbox;

namespace POS.Infrastructure.Http
{
    // The real delivery mechanism behind the "Notifications" consumer
    // (E1) — same shape as ReportingEventPublisher: Notifications.API's
    // own IngestSaleCompletedCommand only declares the two fields (SaleId,
    // Total) it needs, so PayloadJson is forwarded verbatim rather than
    // deserialized and re-serialized, exactly like the Reporting case.
    public class NotificationsEventPublisher : IEventPublisher
    {
        private readonly HttpClient _httpClient;

        public NotificationsEventPublisher(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string ConsumerName => OutboxConsumers.Notifications;

        public async Task<EventPublishResult> PublishAsync(string eventType, string payloadJson, CancellationToken cancellationToken)
        {
            if (eventType != OutboxEventTypes.SaleCompleted)
            {
                return EventPublishResult.Failed($"NotificationsEventPublisher doesn't understand event type '{eventType}'.");
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
                return EventPublishResult.Failed($"could not reach Notifications.API: {ex.Message}");
            }
        }
    }
}

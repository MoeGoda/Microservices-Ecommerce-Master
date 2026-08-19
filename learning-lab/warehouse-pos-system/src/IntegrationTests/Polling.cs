using System.Text.Json;

namespace IntegrationTests
{
    // The outbox/background-consumer pattern (POS/Warehouse -> Reporting/
    // Notifications) is deliberately eventually-consistent — see each
    // service's own OutboxBackgroundService (10s poll interval). An
    // integration test that hits an async projection right after the
    // triggering call is testing the poll delay, not the feature, so
    // every such assertion goes through here instead of a bare Assert.
    public static class Polling
    {
        public static async Task<T> Until<T>(Func<Task<T?>> attempt, Func<T, bool> isReady, string timeoutMessage, int timeoutSeconds = 60)
            where T : class
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            T? last = null;
            while (DateTime.UtcNow < deadline)
            {
                last = await attempt();
                if (last is not null && isReady(last))
                {
                    return last;
                }

                await Task.Delay(1500);
            }

            throw new TimeoutException($"{timeoutMessage} (last value: {(last is null ? "null" : JsonSerializer.Serialize(last))})");
        }
    }
}

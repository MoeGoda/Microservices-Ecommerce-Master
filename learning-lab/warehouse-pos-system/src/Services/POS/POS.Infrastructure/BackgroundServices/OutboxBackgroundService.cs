using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.Application.Features.Outbox;

namespace POS.Infrastructure.BackgroundServices
{
    // The actual poll loop behind OutboxDispatcher — registered via
    // AddHostedService in POS.API's Program.cs (C4). Renamed from
    // SaleCompletedOutboxBackgroundService alongside the outbox's own
    // generalization (D1); the class itself didn't need any behavioral
    // change, since it already only knew how to call DispatchPendingAsync,
    // not anything about what's inside an individual delivery.
    //
    // Each tick opens its own DI scope — IUnitOfWork/DbContext are scoped
    // services, and a BackgroundService's own lifetime is a singleton, so
    // it can't hold onto a scoped dependency across ticks; it has to ask
    // for a fresh one every time, exactly like a scoped-per-request
    // dependency would get a fresh scope per HTTP request.
    public class OutboxBackgroundService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxBackgroundService> _logger;

        public OutboxBackgroundService(IServiceScopeFactory scopeFactory, ILogger<OutboxBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
                    await dispatcher.DispatchPendingAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // A dispatch cycle failing outright (not an individual
                    // delivery's publish failing — that's handled inside
                    // the dispatcher itself) shouldn't take the whole poll
                    // loop down; log it and try again next tick.
                    _logger.LogError(ex, "Outbox dispatch cycle failed");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}

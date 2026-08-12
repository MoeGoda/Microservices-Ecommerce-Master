using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.Application.Features.Outbox;

namespace POS.Infrastructure.BackgroundServices
{
    // The actual poll loop behind SaleCompletedOutboxDispatcher — NOT
    // registered anywhere yet. There's no POS.API Program.cs to call
    // `builder.Services.AddHostedService<SaleCompletedOutboxBackgroundService>()`,
    // the same reason WarehouseContextFactory existed in B1 before
    // Warehouse.API did: this class is ready for that wiring the moment
    // POS.API exists, and is exercised directly (calling DispatchPendingAsync
    // itself, without a real host) by this step's own verification instead.
    //
    // Each tick opens its own DI scope — IUnitOfWork/DbContext are scoped
    // services, and a BackgroundService's own lifetime is a singleton, so
    // it can't hold onto a scoped dependency across ticks; it has to ask
    // for a fresh one every time, exactly like a scoped-per-request
    // dependency would get a fresh scope per HTTP request.
    public class SaleCompletedOutboxBackgroundService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SaleCompletedOutboxBackgroundService> _logger;

        public SaleCompletedOutboxBackgroundService(IServiceScopeFactory scopeFactory, ILogger<SaleCompletedOutboxBackgroundService> logger)
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
                    var dispatcher = scope.ServiceProvider.GetRequiredService<SaleCompletedOutboxDispatcher>();
                    await dispatcher.DispatchPendingAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // A dispatch cycle failing outright (not an individual
                    // entry's publish failing — that's handled inside the
                    // dispatcher itself) shouldn't take the whole poll
                    // loop down; log it and try again next tick.
                    _logger.LogError(ex, "SaleCompleted outbox dispatch cycle failed");
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

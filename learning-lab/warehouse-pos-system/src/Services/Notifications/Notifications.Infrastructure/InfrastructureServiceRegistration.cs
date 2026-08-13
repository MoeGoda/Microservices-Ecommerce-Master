using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Contracts.Infrastructure;
using Notifications.Application.Contracts.Persistence;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Repositories;

namespace Notifications.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<NotificationsContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("NotificationsConnectionString")));

            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IStockLevelSnapshotRepository, StockLevelSnapshotRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Unlike INotificationPusher, IEmailSender's real implementation
            // (MailKit's SmtpClient) is genuinely generic outbound-network
            // plumbing with no dependency on THIS host's own ASP.NET Core
            // pipeline — same reasoning every other Infrastructure-layer
            // HTTP client in this project already follows — so it's
            // registered here, not in Notifications.API.
            services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
            services.AddScoped<IEmailSender, SmtpEmailSender>();

            // No INotificationPusher registration here — deliberately.
            // Unlike every other Infrastructure concern in this project,
            // the real (SignalR) push implementation is tied to THIS
            // service's own ASP.NET Core hosting/request pipeline, not a
            // generic persistence/outbound-HTTP concern, so it's
            // registered where NotificationsHub is mapped: Notifications.API's
            // own Program.cs. See INotificationPusher's own comment.
            return services;
        }
    }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Notifications.Application.Contracts.Infrastructure;

namespace Notifications.Infrastructure.Email
{
    // The real implementation behind IEmailSender — MailKit's SmtpClient
    // against whatever relay SmtpSettings points at (a real one in
    // production, smtp4dev/MailHog/similar for local runs — see the
    // README's "Run it locally" note for this step). No connection
    // pooling/reuse across calls: LowStock notifications are rare enough
    // (this is a crossing-edge alert, not a per-event one — see
    // IngestStockLevelChangedCommandHandler) that a fresh connect/
    // authenticate/send/disconnect per email is simpler than managing a
    // long-lived client, and simplicity is the right tradeoff at this
    // volume.
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendAsync(string subject, string body, CancellationToken cancellationToken)
        {
            if (_settings.Recipients.Length == 0)
            {
                // No alert recipients configured — logged, not thrown.
                // An unconfigured mailing feature shouldn't break the
                // ingestion pipeline that triggers it; the notification
                // itself (DB row + SignalR push) already happened
                // independently of this.
                _logger.LogWarning("SmtpEmailSender: no Smtp:Recipients configured — skipping email '{Subject}'.", subject);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            foreach (var recipient in _settings.Recipients)
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}

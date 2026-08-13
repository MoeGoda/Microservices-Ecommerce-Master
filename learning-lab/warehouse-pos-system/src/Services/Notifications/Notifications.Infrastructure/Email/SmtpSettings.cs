namespace Notifications.Infrastructure.Email
{
    // Bound from configuration's "Smtp" section. Recipients is the fixed
    // alert-recipient list SmtpEmailSender sends every email to — see
    // IEmailSender's own comment for why there's no per-call "to" address.
    public class SmtpSettings
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string FromAddress { get; set; } = null!;
        public string FromName { get; set; } = "WarehousePOS Alerts";
        public string[] Recipients { get; set; } = Array.Empty<string>();
    }
}

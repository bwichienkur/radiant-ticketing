using System.Net;
using System.Net.Mail;
using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Notifications;

public sealed class EmailNotificationPublisher : INotificationPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationPublisher> _logger;

    public EmailNotificationPublisher(IConfiguration configuration, ILogger<EmailNotificationPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishAsync(
        string eventType,
        string title,
        string message,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue("Notifications:Email:Enabled", false))
        {
            return;
        }

        var smtpHost = _configuration["Notifications:Email:SmtpHost"];
        var fromAddress = _configuration["Notifications:Email:FromAddress"];
        var toAddresses = _configuration.GetSection("Notifications:Email:ToAddresses").Get<string[]>() ?? [];

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress) || toAddresses.Length == 0)
        {
            _logger.LogWarning("Email notifications enabled but SMTP settings are incomplete.");
            return;
        }

        var smtpPort = _configuration.GetValue("Notifications:Email:SmtpPort", 587);
        var useSsl = _configuration.GetValue("Notifications:Email:UseSsl", true);
        var username = _configuration["Notifications:Email:Username"];
        var password = _configuration["Notifications:Email:Password"];

        using var mail = new MailMessage
        {
            From = new MailAddress(fromAddress, _configuration["Notifications:Email:FromName"] ?? "EnhancementHub"),
            Subject = $"[{eventType}] {title}",
            Body = $"{message}\n\nEvent: {eventType}\nTime: {DateTime.UtcNow:O}",
            IsBodyHtml = false
        };

        foreach (var to in toAddresses.Where(a => !string.IsNullOrWhiteSpace(a)))
        {
            mail.To.Add(to);
        }

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = useSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        await client.SendMailAsync(mail, cancellationToken);
        _logger.LogInformation("Sent email notification for {EventType} to {Recipients}", eventType, string.Join(", ", toAddresses));
    }
}

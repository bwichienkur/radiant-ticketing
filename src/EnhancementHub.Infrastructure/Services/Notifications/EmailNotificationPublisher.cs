using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Notifications;

public sealed class EmailNotificationPublisher : INotificationPublisher
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationPublisher> _logger;

    public EmailNotificationPublisher(
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<EmailNotificationPublisher> logger)
    {
        _emailSender = emailSender;
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

        var toAddresses = _configuration.GetSection("Notifications:Email:ToAddresses").Get<string[]>() ?? [];
        if (toAddresses.Length == 0)
        {
            _logger.LogWarning("Email notifications enabled but no ToAddresses configured.");
            return;
        }

        var subject = $"[{eventType}] {title}";
        var body = $"{message}\n\nEvent: {eventType}\nTime: {DateTime.UtcNow:O}";

        foreach (var to in toAddresses.Where(a => !string.IsNullOrWhiteSpace(a)))
        {
            try
            {
                await _emailSender.SendAsync(to, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send broadcast email to {Recipient}", to);
            }
        }

        _logger.LogInformation(
            "Sent email notification for {EventType} to {Recipients}",
            eventType,
            string.Join(", ", toAddresses));
    }
}

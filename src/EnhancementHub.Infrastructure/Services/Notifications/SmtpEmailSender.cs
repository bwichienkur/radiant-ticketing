using System.Net;
using System.Net.Mail;
using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Notifications;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(
        string toAddress,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue("Notifications:Email:Enabled", false))
        {
            return;
        }

        var smtpHost = _configuration["Notifications:Email:SmtpHost"];
        var fromAddress = _configuration["Notifications:Email:FromAddress"];

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress) || string.IsNullOrWhiteSpace(toAddress))
        {
            _logger.LogWarning("Email sender skipped because SMTP settings or recipient are incomplete.");
            return;
        }

        var smtpPort = _configuration.GetValue("Notifications:Email:SmtpPort", 587);
        var useSsl = _configuration.GetValue("Notifications:Email:UseSsl", true);
        var username = _configuration["Notifications:Email:Username"];
        var password = _configuration["Notifications:Email:Password"];

        using var mail = new MailMessage
        {
            From = new MailAddress(fromAddress, _configuration["Notifications:Email:FromName"] ?? "EnhancementHub"),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        mail.To.Add(toAddress);

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
        _logger.LogInformation("Sent email to {Recipient}: {Subject}", toAddress, subject);
    }
}

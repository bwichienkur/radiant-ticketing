using EnhancementHub.Domain.Enums;

namespace EnhancementHub.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(
        string toAddress,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}

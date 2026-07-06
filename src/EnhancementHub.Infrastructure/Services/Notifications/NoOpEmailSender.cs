using EnhancementHub.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Notifications;

public sealed class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(
        string toAddress,
        string subject,
        string body,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

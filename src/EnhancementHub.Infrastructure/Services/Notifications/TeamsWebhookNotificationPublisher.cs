using System.Net.Http.Json;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Notifications;

public sealed class TeamsWebhookNotificationPublisher : INotificationPublisher
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TeamsWebhookNotificationPublisher> _logger;

    public TeamsWebhookNotificationPublisher(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TeamsWebhookNotificationPublisher> logger)
    {
        _httpClient = httpClientFactory.CreateClient(InfrastructureServiceExtensions.TeamsWebhookHttpClientName);
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
        if (!_configuration.GetValue("Notifications:Teams:Enabled", false))
        {
            return;
        }

        var webhookUrl = _configuration["Notifications:Teams:WebhookUrl"];
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("Teams notifications enabled but WebhookUrl is not configured.");
            return;
        }

        var payload = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = new object[]
                        {
                            new { type = "TextBlock", text = title, weight = "Bolder", size = "Medium" },
                            new { type = "TextBlock", text = message, wrap = true },
                            new { type = "TextBlock", text = $"Event: {eventType}", isSubtle = true, spacing = "Small" }
                        }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
        {
            Content = JsonContent.Create(payload)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Teams webhook failed: {Status} {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Posted Teams notification for {EventType}", eventType);
    }
}

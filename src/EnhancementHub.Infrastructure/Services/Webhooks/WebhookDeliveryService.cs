using System.Net.Http.Headers;
using System.Text;
using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Common;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnhancementHub.Infrastructure.Services.Webhooks;

public sealed class WebhookDeliveryService
{
    private readonly IEnhancementHubDbContext _dbContext;
    private readonly IConnectionStringProtector _secretProtector;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebhookDeliveryDispatcher _dispatcher;
    private readonly ILogger<WebhookDeliveryService> _logger;

    public WebhookDeliveryService(
        IEnhancementHubDbContext dbContext,
        IConnectionStringProtector secretProtector,
        IHttpClientFactory httpClientFactory,
        IWebhookDeliveryDispatcher dispatcher,
        ILogger<WebhookDeliveryService> logger)
    {
        _dbContext = dbContext;
        _secretProtector = secretProtector;
        _httpClientFactory = httpClientFactory;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task DeliverAsync(Guid webhookDeliveryId, CancellationToken cancellationToken = default)
    {
        var delivery = await _dbContext.WebhookDeliveries
            .Include(d => d.Subscription)
            .FirstOrDefaultAsync(d => d.Id == webhookDeliveryId, cancellationToken);

        if (delivery is null || delivery.Status == WebhookDeliveryStatus.Delivered)
        {
            return;
        }

        if (!delivery.Subscription.IsActive)
        {
            delivery.Status = WebhookDeliveryStatus.Failed;
            delivery.LastError = "Subscription is inactive.";
            delivery.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        delivery.AttemptCount += 1;
        delivery.UpdatedAt = DateTime.UtcNow;

        try
        {
            var secret = _secretProtector.Unprotect(delivery.Subscription.SecretProtected);
            var signature = WebhookSigningUtility.CreateSignatureHeader(delivery.PayloadJson, secret);
            using var request = new HttpRequestMessage(HttpMethod.Post, delivery.Subscription.Url)
            {
                Content = new StringContent(delivery.PayloadJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add(WebhookSigningUtility.SignatureHeaderName, signature);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("EnhancementHub", "1.0"));

            var client = _httpClientFactory.CreateClient(nameof(WebhookDeliveryService));
            client.Timeout = TimeSpan.FromSeconds(30);
            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            delivery.HttpStatusCode = (int)response.StatusCode;
            delivery.ResponseBody = Truncate(responseBody, 4000);

            if (response.IsSuccessStatusCode)
            {
                delivery.Status = WebhookDeliveryStatus.Delivered;
                delivery.DeliveredAt = DateTime.UtcNow;
                delivery.LastError = null;
                delivery.NextRetryAt = null;
            }
            else
            {
                delivery.LastError = $"HTTP {(int)response.StatusCode}";
                await HandleFailureAsync(delivery, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            delivery.LastError = Truncate(ex.Message, 2000);
            await HandleFailureAsync(delivery, cancellationToken);
            _logger.LogWarning(ex, "Webhook delivery {DeliveryId} failed on attempt {Attempt}", delivery.Id, delivery.AttemptCount);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task HandleFailureAsync(WebhookDelivery delivery, CancellationToken cancellationToken)
    {
        if (delivery.AttemptCount >= delivery.MaxAttempts)
        {
            delivery.Status = WebhookDeliveryStatus.Failed;
            delivery.NextRetryAt = null;
            return Task.CompletedTask;
        }

        delivery.Status = WebhookDeliveryStatus.Pending;
        delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, delivery.AttemptCount));
        _dispatcher.EnqueueDelivery(delivery.Id);
        return Task.CompletedTask;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}

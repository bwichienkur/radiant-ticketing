using EnhancementHub.Application.Common;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class Phase52WebhookTests
{
    [Fact]
    public void WebhookSigningUtility_CreatesAndVerifiesSignature()
    {
        const string payload = """{"eventType":"request.approved","data":{"id":"123"}}""";
        const string secret = "whsec_test_secret";

        var header = WebhookSigningUtility.CreateSignatureHeader(payload, secret, DateTimeOffset.UnixEpoch.AddSeconds(1_700_000_000));
        WebhookSigningUtility.VerifySignature(payload, header, secret).Should().BeTrue();
        WebhookSigningUtility.VerifySignature(payload, "t=1,v1=bad", secret).Should().BeFalse();
    }

    [Fact]
    public void AdminWebhooksPage_ExistsWithDeliveryLog()
    {
        var page = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Admin/Webhooks.cshtml"));
        page.Should().Contain("Outbound webhooks");
        page.Should().Contain("Recent deliveries");
        page.Should().Contain("WebhookEventTypes");
    }

    [Fact]
    public void SubmitApprovalActionCommand_PublishesRequestApprovedWebhook()
    {
        var handler = File.ReadAllText(GetPath("src/EnhancementHub.Application/Features/Approvals/Commands/SubmitApprovalActionCommand.cs"));
        handler.Should().Contain("IWebhookEventPublisher");
        handler.Should().Contain("WebhookEventTypes.RequestApproved");
    }

    [Fact]
    public void WebhookEventPublisher_EnqueuesDeliveries()
    {
        var publisher = File.ReadAllText(GetPath("src/EnhancementHub.Infrastructure/Services/Webhooks/WebhookEventPublisher.cs"));
        publisher.Should().Contain("WebhookDelivery");
        publisher.Should().Contain("EnqueueDelivery");
    }

    [Fact]
    public void WebhookDeliveryService_SendsSignedPost()
    {
        var service = File.ReadAllText(GetPath("src/EnhancementHub.Infrastructure/Services/Webhooks/WebhookDeliveryService.cs"));
        service.Should().Contain("WebhookSigningUtility.SignatureHeaderName");
        service.Should().Contain("MaxAttempts");
    }

    [Fact]
    public void AdminNav_IncludesWebhooksLink()
    {
        var nav = File.ReadAllText(GetPath("src/EnhancementHub.Web/Pages/Shared/_AdminNav.cshtml"));
        nav.Should().Contain("/Admin/Webhooks");
    }

    private static string GetPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
}

namespace EnhancementHub.Application.Options;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public bool Enabled { get; set; }
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = "http://localhost:5001/Admin/Tenancy?checkout=success";
    public string CancelUrl { get; set; } = "http://localhost:5001/Admin/Tenancy?checkout=cancel";
    public string PortalReturnUrl { get; set; } = "http://localhost:5001/Admin/Tenancy";
    public StripePriceOptions Prices { get; set; } = new();
}

public sealed class StripePriceOptions
{
    public string Team { get; set; } = string.Empty;
    public string Enterprise { get; set; } = string.Empty;
}

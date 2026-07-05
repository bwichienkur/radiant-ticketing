# EnhancementHub — Stripe Billing (Phase 28)

Phase 28 connects the commercial platform to Stripe for self-service upgrades, subscription lifecycle management, and trial enforcement.

---

## Architecture

| Concern | Implementation |
|---------|----------------|
| Checkout | Stripe Checkout Sessions via `IStripeBillingService.CreateCheckoutSessionAsync` |
| Customer portal | Stripe Billing Portal for plan changes and invoices |
| Webhooks | `POST /api/webhooks/stripe` with HMAC signature verification |
| Tenant linkage | `Tenant.StripeCustomerId`, `StripeSubscriptionId`, `SubscriptionStatus` |
| Trial enforcement | `ITenantBillingService.EnsureWithinLimitsAsync` blocks expired trials |

---

## Configuration

`Stripe` section in `appsettings.json`:

```json
"Stripe": {
  "Enabled": true,
  "SecretKey": "sk_test_...",
  "WebhookSecret": "whsec_...",
  "PublishableKey": "pk_test_...",
  "SuccessUrl": "https://app.example.com/Admin/Tenancy?checkout=success",
  "CancelUrl": "https://app.example.com/Admin/Tenancy?checkout=cancel",
  "PortalReturnUrl": "https://app.example.com/Admin/Tenancy",
  "Prices": {
    "Team": "price_team_monthly",
    "Enterprise": "price_enterprise_monthly"
  }
}
```

Create matching recurring prices in the Stripe Dashboard and point webhooks to `/api/webhooks/stripe`.

Handled webhook events:

- `checkout.session.completed` — links customer/subscription and upgrades plan
- `customer.subscription.updated` — syncs status and renewal date
- `customer.subscription.deleted` — marks subscription canceled

---

## API endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/billing/checkout` | Admin + tenant | Create Stripe Checkout session (`plan`: `Team` or `Enterprise`) |
| `POST` | `/api/billing/portal` | Admin + tenant | Open Stripe customer portal |
| `POST` | `/api/webhooks/stripe` | Anonymous (signed) | Stripe webhook receiver |

---

## Admin UI

`/Admin/Tenancy` shows subscription status, trial expiry, and upgrade CTAs when Stripe is enabled. Platform admins without tenant context continue to see the cross-tenant list.

---

## Trial enforcement

When a tenant remains on the `Trial` plan after `TrialEndsAt` and has no active Stripe subscription, analysis and other metered operations are blocked with a clear upgrade message.

Paid plans (`Team`, `Enterprise`) or active/trialing Stripe subscriptions restore access.

---

## Related

- [COMMERCIAL_PLATFORM.md](COMMERCIAL_PLATFORM.md) — Phase 26 multi-tenant foundation
- [PRICING.md](PRICING.md) — plan tiers
- [PHASES.md](PHASES.md) — Phase 28 summary

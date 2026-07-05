# EnhancementHub — Commercial Platform (Phase 26)

Phase 26 introduces multi-tenant data isolation, usage metering, self-service trial signup, and regional tenant metadata for a commercial SaaS deployment model.

---

## Architecture

| Concern | Implementation |
|---------|----------------|
| Data isolation | Row-level via `TenantId` on `Users` and `Teams`; application visibility filtered through team ownership |
| Tenant context | JWT / cookie claim `tenant_id`; `ICurrentTenantService` resolves current tenant |
| Metering | Monthly `TenantUsageSnapshot` — applications, analyses, storage estimate |
| Billing limits | Plan-based limits in `Commercial` configuration (`Trial`, `Team`, `Enterprise`) |
| Self-service signup | `POST /api/tenants/register` and `/Account/Signup` |
| Regional deployment | `TenantRegion` (`US`, `EU`, `APAC`) stored per tenant for routing/residency metadata |

Platform admins without a `tenant_id` claim retain cross-tenant visibility (existing dev admin pattern).

---

## Configuration

`Commercial` section in `appsettings.json`:

```json
"Commercial": {
  "Enabled": true,
  "SelfServiceSignupEnabled": true,
  "TrialDays": 14,
  "DefaultRegion": "US",
  "TrialLimits": { "MaxApplications": 3, "MaxAnalysesPerMonth": 50, "MaxStorageMegabytes": 500 },
  "TeamLimits": { "MaxApplications": 25, "MaxAnalysesPerMonth": 500, "MaxStorageMegabytes": 5000 },
  "EnterpriseLimits": { "MaxApplications": 500, "MaxAnalysesPerMonth": 10000, "MaxStorageMegabytes": 100000 }
}
```

Set `SelfServiceSignupEnabled` to `false` in production until billing integration is ready.

---

## API endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/tenants/register` | Anonymous | Create trial tenant, admin user, default team, usage snapshot |
| `GET` | `/api/tenants/current/billing` | Bearer / cookie | Current tenant plan, limits, and usage |
| `GET` | `/api/tenants` | Admin | List all tenants (platform admin) |

---

## Signup flow

1. User submits organization name, slug, admin credentials, and region on `/Account/Signup`.
2. `RegisterTenantCommand` creates tenant (`Trial` plan), admin user, default team, and initial usage snapshot.
3. User is signed in with `tenant_id` claim and redirected to the dashboard.

Trial tenants receive JWT tokens including `tenant_id` for API calls.

---

## Metering and limits

| Metric | Source |
|--------|--------|
| Applications | Count of applications owned by teams in the tenant |
| Analyses | Incremented on successful AI analysis (`TriggerAiAnalysisCommand`) |
| Storage | Attachment count × 1 MB estimate (per tenant submitters) |

Before analysis runs, `ITenantBillingService.EnsureWithinLimitsAsync` blocks requests that exceed plan limits.

---

## Admin UI

- `/Admin/Tenancy` — tenant billing summary for tenant-scoped admins; platform admins without tenant context see all tenants.
- `/Account/Signup` — self-service trial registration (anonymous).

---

## Migration and seed data

Migration `Phase26CommercialPlatform`:

- Adds `Tenants`, `TenantUsageSnapshots`, `Users.TenantId`, `Teams.TenantId`
- Backfills existing rows to default tenant `99999999-9999-9999-9999-999999999999` (`slug: default`)

Dev seeder assigns the default tenant to the development admin user and demo team.

---

## Regional deployment (EU data residency)

`TenantRegion` is persisted at signup and exposed in billing/admin surfaces. Actual regional routing (separate databases, geo-replicated storage) is an operational deployment concern — configure connection strings and storage per region in your environment templates.

---

## Related

- [ROADMAP.md](ROADMAP.md) — Horizon 4.4
- [PHASES.md](PHASES.md) — Phase 26 summary
- [PRICING.md](PRICING.md) — plan tiers

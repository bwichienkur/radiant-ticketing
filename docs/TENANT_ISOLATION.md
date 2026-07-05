# EnhancementHub — Tenant Isolation (Phase 29)

Phase 29 adds optional **schema-per-tenant** isolation for regulated workloads on PostgreSQL, complementing the row-level `TenantId` filtering introduced in Phase 26.

---

## Isolation modes

| Mode | Description |
|------|-------------|
| `SharedRowLevel` | Default — all tenants share the `public` schema; `TenantId` filters enforce boundaries |
| `DedicatedSchema` | Tenant data lives in a dedicated PostgreSQL schema (`tenant_{slug}`); connections use `SET search_path` |

Control-plane tables (`Tenants`, `TenantUsageSnapshots`, `__EFMigrationsHistory`) always remain in `public`.

---

## Configuration

`TenantIsolation` section in `appsettings.json`:

```json
"TenantIsolation": {
  "Enabled": true,
  "SchemaPrefix": "tenant_",
  "AutoProvisionEnterprise": true,
  "AutoProvisionEuRegion": false
}
```

| Setting | Description |
|---------|-------------|
| `Enabled` | Master switch for provisioning and admin UI |
| `SchemaPrefix` | Prefix for generated schema names |
| `AutoProvisionEnterprise` | Provision schema when tenant upgrades to Enterprise (Stripe webhook) |
| `AutoProvisionEuRegion` | Provision schema on EU region signup |

**Requires PostgreSQL** for actual schema creation. SQLite dev environments mark tenants as provisioned without creating schemas.

---

## Provisioning flow

1. Admin clicks **Provision dedicated schema** on `/Admin/Tenancy`, or auto-provision triggers on Enterprise upgrade.
2. `TenantSchemaProvisioner` runs `CREATE SCHEMA` and clones table structures from `public` (excluding control-plane tables).
3. Tenant record updated: `IsolationMode=DedicatedSchema`, `DatabaseSchemaName`, `SchemaProvisionedAt`.
4. `TenantIsolationMiddleware` resolves the active schema per request; `TenantSearchPathConnectionInterceptor` sets `search_path` on PostgreSQL connections.

Row-level `TenantId` filters remain as defense in depth.

---

## API endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/tenants/current/isolation` | Admin + tenant | Isolation mode and schema status |
| `POST` | `/api/tenants/current/isolation/provision` | Admin + tenant | Provision dedicated schema |

---

## Operational notes

- Run provisioning during a maintenance window for large tenants — schema clone iterates all public tables.
- Backup/restore per tenant schema is possible with `pg_dump --schema=tenant_acme`.
- For full database-per-tenant isolation, configure separate connection strings per region in your deployment templates (operational concern beyond this phase).

---

## Related

- [COMMERCIAL_PLATFORM.md](COMMERCIAL_PLATFORM.md) — Phase 26 row-level multi-tenancy
- [STRIPE_BILLING.md](STRIPE_BILLING.md) — Enterprise auto-provision hook
- [PHASES.md](PHASES.md) — Phase 29 summary

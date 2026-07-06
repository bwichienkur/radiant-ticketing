# API Versioning Policy

EnhancementHub exposes REST endpoints under `/api/*` with automatic duplication at `/api/v1/*` via `ApiV1RouteConvention`.

Related: [SECURITY.md](SECURITY.md) · [PHASES.md](PHASES.md)

---

## Current version

**Stable version:** `v1` (July 2026)

All controllers registered with `[Route("api/[controller]")]` or explicit `api/...` routes are also reachable at `/api/v1/...` with identical behavior.

Examples:

| Unversioned | Versioned |
|-------------|-----------|
| `GET /api/EnhancementRequests` | `GET /api/v1/EnhancementRequests` |
| `GET /api/audit/export` | `GET /api/v1/audit/export` |
| `GET /api/policies` | `GET /api/v1/policies` |

SCIM endpoints (`/scim/v2/Users`) follow the SCIM protocol version and are **not** duplicated under `/api/v1`.

---

## Client guidance

1. **New integrations** should call `/api/v1/*` explicitly.
2. **Existing clients** on `/api/*` continue to work; no breaking change is planned for v1.
3. **SPA BFF** routes (`/web-api/spa/*`) are not versioned; they are tied to the Web app release.

---

## Deprecation policy

When `/api/v2` is introduced:

| Milestone | Action |
|-----------|--------|
| v2 GA | v1 remains available for **12 months** |
| Month 6 | `Sunset` header on v1 responses; changelog + release notes |
| Month 12 | v1 routes return `410 Gone` with link to migration guide |

Breaking changes (removed fields, renamed routes, semantic changes) require a new major version. Additive changes (new optional fields, new endpoints) ship in the current version.

---

## Authentication

Versioned and unversioned routes share the same auth:

- JWT bearer (`Authorization: Bearer`)
- Service API key (`X-Api-Key`)
- SCIM bearer token (SCIM routes only)

---

## Changelog

| Date | Change |
|------|--------|
| 2026-07-06 | Initial policy — v1 stable, `/api/v1` alias convention documented |

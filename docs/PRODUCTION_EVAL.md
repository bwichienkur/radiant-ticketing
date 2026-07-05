# Production evaluation profile

One-command Docker stack for **buyer demos and production-like evaluation** — PostgreSQL, PgVector, Hangfire background jobs, and optional OpenAI analysis.

## Quick start

```bash
# From repository root
export OPENAI_API_KEY=sk-...   # optional — mock AI used when unset

docker compose -f docker-compose.yml -f docker-compose.eval.yml up --build
```

| Service | URL | Notes |
|---------|-----|-------|
| **Web UI** | http://localhost:5001 | Login: `admin@enhancementhub.dev` / `password123` |
| **API** | http://localhost:5075 | JWT + `/api/v1/*` |
| **Worker** | http://localhost:5076 | Hangfire dashboard (Development) |
| **PostgreSQL** | localhost:5432 | `enhancementhub` / `enhancementhub` |

## What differs from default `docker compose up`

| Setting | Default dev compose | Eval profile |
|---------|---------------------|--------------|
| Database | PostgreSQL | PostgreSQL (same) |
| Vector search | PgVector | PgVector |
| Background jobs | Polling (worker default) | **Hangfire** on worker |
| Qdrant | Optional profile `scale` | Not required (PgVector) |
| Demo seed | On Web startup | On Web startup |
| OpenAI | Empty → mock AI | Set `OPENAI_API_KEY` for real analysis |

## Optional: enable Qdrant (large vector workloads)

```bash
docker compose -f docker-compose.yml -f docker-compose.eval.yml --profile scale up --build
```

Set `VectorSearch__Provider=Qdrant` on api/web/worker if switching from PgVector.

## Smoke tests (CI)

Playwright smoke tests cover login, dashboard, and the four React SPAs:

```bash
cd tests/e2e && npm install && npx playwright install chromium
node ../../scripts/run-e2e-smoke.mjs
```

Runs automatically on pull requests via `.github/workflows/e2e-smoke.yml`.

## Stopping and reset

```bash
docker compose -f docker-compose.yml -f docker-compose.eval.yml down
docker volume rm enhancementhub_postgres_data   # full reset
```

## Production hardening checklist (beyond eval)

Before a real pilot deployment, also configure:

- Entra ID OIDC (`Authentication:OpenIdConnect`)
- Stripe billing (`Stripe:Enabled`)
- Tenant isolation for multi-tenant SaaS (`TenantIsolation:Enabled`)
- Observability (`Observability:Enabled` + OTLP endpoint)
- Secrets via environment or vault — never commit keys

See [DEPLOYMENT.md](DEPLOYMENT.md), [SECURITY.md](SECURITY.md), and [HA_ARCHITECTURE.md](HA_ARCHITECTURE.md).

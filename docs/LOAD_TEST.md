# EnhancementHub — Load Testing (Phase 35 / Phase 55)

Horizon 3 exit criteria from [ROADMAP.md](ROADMAP.md):

| Target | Value |
|--------|-------|
| Repositories indexed | 200 |
| Concurrent users | 500 |
| AI analyses per hour | 50 |

This document covers the **staging checklist**, k6 profiles, seeding, and success criteria for proving Horizon 3 scale.

---

## Staging environment checklist (Phase 55.1)

Provision before running `k6-horizon3.js` at full scale:

| Component | Minimum | Notes |
|-----------|---------|-------|
| API instances | **2** | Behind load balancer; shared Data Protection key ring |
| Worker instances | **2** | Hangfire `default` + `indexing` queues; no duplicate discovery |
| PostgreSQL | **1 HA cluster** | Primary + read replica for reporting (optional) |
| pgvector | **Enabled** | `CREATE EXTENSION vector;` on primary — or Qdrant/Azure Search for vector offload |
| Redis / Blob | Optional | Session/cache if enabled in your deployment |
| Object storage | S3-compatible | Attachments + repo archives at scale |

### Recommended `appsettings.Staging.json` overrides

```json
{
  "Database": { "Provider": "Npgsql" },
  "ConnectionStrings": {
    "Default": "Host=postgres;Database=enhancementhub;Username=...;Password=...",
    "Reporting": "Host=postgres-replica;Database=enhancementhub;..."
  },
  "DatabaseScaling": {
    "MaxPoolSize": 150,
    "MinPoolSize": 10,
    "SchemaScanMaxConcurrency": 2
  },
  "BackgroundJobs": { "Provider": "Hangfire" },
  "VectorSearch": { "Provider": "PgVector" }
}
```

### Pre-flight checks

- [ ] Both API instances pass `GET /health/ready`
- [ ] Both workers connected to same Hangfire PostgreSQL schema
- [ ] `dotnet run --project tests/EnhancementHub.LoadTestSeeder -- --repositories 200` completed
- [ ] Demo/admin user exists for k6 login
- [ ] AI budget limits configured for 50 analyses/hour soak

---

## Quick smoke test

```bash
# Install k6: https://k6.io/docs/get-started/installation/
export BASE_URL=http://localhost:5075
export VUS=10
export DURATION=30s
k6 run tests/load/k6-smoke.js
```

The smoke script exercises:

- `GET /health/ready`
- `GET /api/v1/EnhancementRequests?page=1&pageSize=25` (authenticated)
- `GET /api/v1/applications` (authenticated)

For the latest measured run, see [LOAD_TEST_RESULTS.md](LOAD_TEST_RESULTS.md).

To run with API lifecycle managed automatically:

```bash
node scripts/run-load-test-smoke.mjs
```

---

## Horizon 3 profile

Use `tests/load/k6-horizon3.js` for staged load toward exit criteria:

```bash
export BASE_URL=https://staging.example.com
export TEST_USER=admin@enhancementhub.dev
export TEST_PASSWORD=password123
export K6_PROFILE=full
k6 run tests/load/k6-horizon3.js
```

| Stage | VUs | Duration | Purpose |
|-------|-----|----------|---------|
| Ramp | 0 → 100 | 5m | Warm connections |
| Sustain | 500 | 10m | Concurrent operator load |
| Spike | 500 → 50 | 2m | Cool down |

**CI / regression profile** (30 VUs, ~3.5 min):

```bash
export K6_PROFILE=ci
node scripts/run-load-test-horizon3.mjs
```

Read-path threshold: **p95 < 500ms** for tagged `endpoint:read` requests.

Record p95 latency for request list and health endpoints. Verify Hangfire dashboard shows no duplicate indexing jobs for the same repository (`DisableConcurrentExecution` on indexing executor).

---

## Synthetic data seeding

Before a 200-repo test, seed applications via the load-test seeder:

```bash
dotnet run --project tests/EnhancementHub.LoadTestSeeder -- --repositories 200
```

The seeder creates tenant-scoped applications and repository metadata (no real git clones) for list/map API stress.

---

## Success checklist

- [ ] p95 `GET /api/v1/EnhancementRequests` < 500ms at 500 VUs (staging)
- [ ] No duplicate indexing jobs for the same repository under parallel discovery queue
- [ ] 50 `POST /api/v1/EnhancementRequests/{id}/analyze` per hour without worker crash
- [ ] Postgres connection pool stable (`DatabaseScaling:MaxPoolSize` tuned, no timeout storms)

---

## CI integration (Phase 55.4)

| Trigger | Workflow | Profile |
|---------|----------|---------|
| PR / push (`ci.yml`) | Optional local smoke via dev scripts | `k6-smoke.js` |
| Nightly schedule | `.github/workflows/load-nightly.yml` | `k6-smoke.js` + `k6-horizon3.js` (`K6_PROFILE=ci`) |
| Manual dispatch | `load-nightly.yml` | `ci` or `full` |

---

*Phase 35 harness · Phase 55 staging proof & bottleneck tuning.*

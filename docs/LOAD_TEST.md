# EnhancementHub — Load Testing (Phase 35)

Horizon 3 exit criteria from [ROADMAP.md](ROADMAP.md):

| Target | Value |
|--------|-------|
| Repositories indexed | 200 |
| Concurrent users | 500 |
| AI analyses per hour | 50 |

This phase adds a **repeatable load-test harness** (k6) and documents how to prove scale before production pilots.

---

## Prerequisites

- Running API at `BASE_URL` (default `http://localhost:5075`)
- Valid JWT or use the login helper in the smoke script
- PostgreSQL + Worker + Hangfire for realistic job throughput

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

- `GET /health`
- `GET /api/v1/EnhancementRequests` (authenticated)
- `GET /api/v1/applications` (authenticated)

---

## Horizon 3 profile (manual run)

Use `tests/load/k6-horizon3.js` for staged load toward exit criteria:

```bash
export BASE_URL=https://staging.example.com
export TEST_USER=architect@pilot.local
export TEST_PASSWORD=PilotPassword1!
k6 run tests/load/k6-horizon3.js
```

| Stage | VUs | Duration | Purpose |
|-------|-----|----------|---------|
| Ramp | 0 → 100 | 5m | Warm connections |
| Sustain | 500 | 10m | Concurrent operator load |
| Spike | 500 → 50 | 2m | Cool down |

Record p95 latency for request list and analysis trigger. Verify Hangfire dashboard shows no duplicate discovery jobs (idempotent queue).

---

## Synthetic data seeding

Before a 200-repo test, seed applications via API or SQL:

```bash
dotnet run --project tests/EnhancementHub.LoadTestSeeder
```

The seeder creates tenant-scoped applications and repository metadata (no real git clones) for list/map API stress.

---

## Success checklist

- [ ] p95 `GET /api/v1/EnhancementRequests` < 500ms at 500 VUs
- [ ] No job duplication under parallel discovery queue
- [ ] 50 `POST /api/v1/EnhancementRequests/{id}/analyze` per hour without worker crash
- [ ] Postgres connection pool stable (no timeout storms)

---

*Phase 35 — load-test harness & Horizon 3 scale proof.*

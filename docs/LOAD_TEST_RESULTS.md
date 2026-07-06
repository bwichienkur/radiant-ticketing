# EnhancementHub — Load Test Results

## Smoke profile (`tests/load/k6-smoke.js`)

Measured on **2026-07-06** against a local Development API instance (SQLite, fresh DB).

| Setting | Value |
|---------|-------|
| Virtual users | 10 |
| Duration | 30s |
| Base URL | `http://127.0.0.1:5075` |
| Credentials | `admin@enhancementhub.dev` / `password123` |

### Endpoints exercised

- `GET /health/ready`
- `GET /api/v1/EnhancementRequests?page=1&pageSize=25` (authenticated)
- `GET /api/v1/applications` (authenticated)

### Results

| Metric | Result | Threshold |
|--------|--------|-----------|
| HTTP failure rate | **0.00%** | < 5% |
| p95 latency | **~10 ms** | < 2000 ms |
| Iterations | 300 | — |
| Throughput | ~29.5 req/s | — |

All checks passed.

### How to reproduce

```bash
node scripts/run-load-test-smoke.mjs
```

Raw output: `artifacts/load-test/k6-smoke-*.txt`

---

## Horizon 3 CI profile (`tests/load/k6-horizon3.js`, `K6_PROFILE=ci`)

Measured on **2026-07-06** against a local Development API instance (SQLite, fresh DB). This is the **CI regression profile** (30 VUs, ~3.5 min) — not the full staging profile (500 VUs / 17 min).

| Setting | Value |
|---------|-------|
| Profile | `ci` |
| Peak VUs | 30 |
| Duration | ~3m 30s |
| Base URL | `http://127.0.0.1:5075` |
| Credentials | `admin@enhancementhub.dev` / `password123` |

### Endpoints exercised

- `GET /health/ready` (tagged `endpoint:read`)
- `GET /api/v1/EnhancementRequests?page=1&pageSize=25` (tagged `endpoint:read`)
- `GET /api/v1/applications` (tagged `endpoint:read`)

### Results

| Metric | Result | Threshold |
|--------|--------|-----------|
| HTTP failure rate | **0.00%** | < 2% |
| Overall p95 latency | **2.14 ms** | < 2000 ms |
| Read-path p95 (`endpoint:read`) | **2.14 ms** | **< 500 ms** |
| Iterations | 9,857 | — |
| Throughput | ~141 req/s | — |

All thresholds passed. Read-path p95 is well under the Horizon 3 target of 500 ms.

### How to reproduce

```bash
node scripts/run-load-test-horizon3.mjs
# or: K6_PROFILE=ci k6 run tests/load/k6-horizon3.js
```

Raw output: `artifacts/load-test/k6-horizon3-ci-*.txt`

---

## Full Horizon 3 staging profile (500 VUs)

The **full** profile (`K6_PROFILE=full`, 500 VUs, 17 minutes) must be run against the [staging checklist](LOAD_TEST.md) topology:

- 2 API instances behind a load balancer
- 2 Worker instances (Hangfire)
- PostgreSQL + pgvector (or Qdrant offload)
- 200 repositories seeded via `tests/EnhancementHub.LoadTestSeeder`

Record staging results in this file after the pilot run. Local SQLite CI results above establish regression baselines and validate read-path thresholds at moderate concurrency.

---

## Phase 55 bottleneck fixes applied

| Area | Change |
|------|--------|
| API list pagination | Default `pageSize=25`; no unbounded list loads |
| Indexing jobs | `DisableConcurrentExecution` + distinct repository dispatch |
| Connection pools | Default `DatabaseScaling:MaxPoolSize` raised to 150 |

---

## Notes

- Local SQLite results differ from PostgreSQL staging topology; use CI profile for regression, full profile for procurement proof.
- Delete `enhancementhub.db` before local runs if DevDataSeeder hits duplicate-key errors on a reused database file.

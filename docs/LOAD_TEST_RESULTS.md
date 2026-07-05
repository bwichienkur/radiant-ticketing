# EnhancementHub — Load Test Results

Measured on **2026-07-05** against a local Development API instance (SQLite, seeded demo data).

## Smoke profile (`tests/load/k6-smoke.js`)

| Setting | Value |
|---------|-------|
| Virtual users | 10 |
| Duration | 30s |
| Base URL | `http://127.0.0.1:5075` |
| Credentials | `admin@enhancementhub.dev` / `password123` |

### Endpoints exercised

- `GET /health/ready`
- `GET /api/v1/EnhancementRequests` (authenticated)
- `GET /api/v1/applications` (authenticated)

### Results

| Metric | Result | Threshold |
|--------|--------|-----------|
| HTTP failure rate | **0.00%** | < 5% |
| p95 latency | **9.2 ms** | < 2000 ms |
| Iterations | 300 | — |
| Throughput | ~29.5 req/s | — |

All checks passed (900/900).

### How to reproduce

```bash
node scripts/run-load-test-smoke.mjs
```

Or against a running API:

```bash
export BASE_URL=http://localhost:5075
export TEST_USER=admin@enhancementhub.dev
export TEST_PASSWORD=password123
k6 run tests/load/k6-smoke.js
```

Raw output is saved under `artifacts/load-test/`.

## Notes

- This smoke profile validates API responsiveness under light concurrent load; it is **not** the Horizon 3 target (500 VUs / 10 minutes). Use `tests/load/k6-horizon3.js` for staged scale tests against staging infrastructure.
- Local SQLite results will differ from PostgreSQL production topology; treat these numbers as a regression baseline for CI, not pilot SLA proof.

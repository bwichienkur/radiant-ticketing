# EnhancementHub — Observability

Phase 22 OpenTelemetry traces, metrics, and dashboard templates.

---

## Enable OpenTelemetry

```json
"Observability": {
  "Enabled": true,
  "ServiceName": "EnhancementHub",
  "OtlpEndpoint": "http://otel-collector:4317",
  "EnablePrometheusMetrics": true,
  "InstrumentAspNetCore": true,
  "InstrumentHttpClient": true,
  "InstrumentEntityFramework": true,
  "InstrumentBackgroundJobs": true
}
```

Set per-host service names in code (`EnhancementHub.Api`, `EnhancementHub.Web`, `EnhancementHub.Worker`) for trace attribution.

### Exporters

| Exporter | Config | Endpoint |
|----------|--------|----------|
| OTLP (traces + metrics) | `Observability:OtlpEndpoint` | gRPC `:4317` or HTTP `:4318` |
| Prometheus scrape | `EnablePrometheusMetrics=true` | `GET /metrics` on API and Worker |

---

## Local stack (Docker)

Start the observability profile alongside the app:

```bash
docker compose -f docker-compose.yml -f docker-compose.observability.yml --profile observability up
```

Includes OpenTelemetry Collector, Prometheus, and Grafana (port 3000).

Import dashboard: `deploy/observability/grafana/enhancementhub-dashboard.json`

---

## Background job metrics

When `InstrumentBackgroundJobs=true`, Hangfire jobs emit:

| Metric | Description |
|--------|-------------|
| `enhancementhub.job.duration.seconds` | Histogram of job duration |
| `enhancementhub.job.completed.total` | Successful completions |
| `enhancementhub.job.failed.total` | Failures |

Traces use activity source `EnhancementHub` with `hangfire.job.id` and `hangfire.job.type` tags.

---

## Datadog

See `deploy/observability/datadog/monitors.yaml` for sample monitor definitions.

Point OTLP to the Datadog Agent:

```json
"OtlpEndpoint": "http://datadog-agent:4317"
```

---

## Admin visibility

- UI: `/Admin/Observability`
- API: `GET /api/admin/observability/status`

---

*See [HA_ARCHITECTURE.md](HA_ARCHITECTURE.md) for multi-instance deployment.*

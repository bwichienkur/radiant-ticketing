# Data Layer Scaling Guide

Guidance for Phase 20 large-portfolio deployments (100+ repositories, 500+ users).

---

## Vector search offload

| Provider | When to use |
|----------|-------------|
| **InMemory** | Local dev only — not shared across API/Worker processes |
| **PgVector** | Moderate scale with PostgreSQL already deployed |
| **Qdrant** | Recommended default for large deployments (100+ repos) |
| **AzureSearch** | Azure-native estates with existing Search service |

### Qdrant (recommended at scale)

```json
"VectorSearch": {
  "Provider": "Qdrant",
  "Dimensions": 64,
  "Qdrant": {
    "Url": "http://qdrant:6333",
    "Collection": "indexed_files"
  }
}
```

Docker Compose includes an optional `qdrant` service — start with `docker compose --profile scale up`.

Validate configuration at `/Admin/DataScaling` or `GET /api/admin/data-scaling/status`.

---

## Read replica for reporting

Dashboard and AI usage queries use `IReportingDbContext`, which connects to `ConnectionStrings:Reporting` when configured. Falls back to `Default` when not set.

```json
"ConnectionStrings": {
  "Default": "Host=primary.postgres;...",
  "Reporting": "Host=replica.postgres;..."
}
```

Reporting queries run with **no-tracking** semantics on the read connection.

---

## Connection pool tuning

```json
"DatabaseScaling": {
  "MaxPoolSize": 100,
  "MinPoolSize": 0,
  "ConnectionTimeoutSeconds": 15,
  "SchemaScanMaxConcurrency": 2
}
```

- **MaxPoolSize** — applied to Npgsql connections (API, Worker, reporting)
- **SchemaScanMaxConcurrency** — limits parallel database schema scans in the Worker

---

## Audit log and AI prompt run archival

Retention policies (`Retention` config section) purge expired `AiPromptRun` records and attachments.

Enable archive-before-delete to export JSON snapshots before purge:

```json
"Retention": {
  "Enabled": true,
  "AiPromptRunsDays": 365,
  "AttachmentsDays": 180,
  "ArchiveAiPromptRunsBeforeDelete": true,
  "ArchivePath": "/data/archives/ai-prompt-runs"
}
```

**Audit logs** are not auto-deleted — use `/Audit` CSV/JSON export on a schedule. For very high volume, partition or archive to cold storage using your Postgres tooling.

Monitor counts at `/Admin/DataScaling`.

---

## Production checklist

- [ ] Vector provider is Qdrant or AzureSearch (not InMemory)
- [ ] `ConnectionStrings:Reporting` points to read replica (if available)
- [ ] `DatabaseScaling:MaxPoolSize` sized for expected concurrent connections
- [ ] `Retention:ArchiveAiPromptRunsBeforeDelete=true` in production
- [ ] Archive path on durable volume or object storage mount

See also [DEPLOYMENT.md](DEPLOYMENT.md) and [SECURITY.md](SECURITY.md).

*Last updated: July 2026*

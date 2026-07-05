# System Intelligence Performance Guide

Phase 21 optimizations for large applications with deep graphs and frequent re-indexing.

---

## Configuration

```json
"SystemIntelligence": {
  "IncrementalGraphEnabled": true,
  "GraphQueryDefaultPageSize": 200,
  "GraphQueryMaxPageSize": 500,
  "GraphQueryDefaultMaxDepth": 4,
  "DocumentationCacheEnabled": true,
  "DocumentationCacheTtlMinutes": 60,
  "DiffOnlyDriftEnabled": true,
  "ScheduledDriftScanIntervalHours": 24
}
```

| Setting | Purpose |
|---------|---------|
| **IncrementalGraphEnabled** | Rebuild only repositories indexed since last graph update; refresh DB nodes when schema scans change |
| **GraphQueryDefaultPageSize / MaxPageSize** | Cap nodes returned per system map page |
| **GraphQueryDefaultMaxDepth** | BFS depth limit from root node |
| **DocumentationCacheEnabled / TtlMinutes** | Cache markdown + Mermaid exports; invalidate when repo index or schema scan timestamps change |
| **DiffOnlyDriftEnabled** | Skip drift detection when live DB and indexed code unchanged since `LastDriftScanAt` |

---

## Incremental system graph

When `IncrementalGraphEnabled=true` (default):

1. Compare each repository's `LastIndexedAt` to the latest graph node `LastUpdatedAt`.
2. Rebuild subgraphs only for stale repositories.
3. Refresh table/column nodes when any `DatabaseConnection.LastScannedAt` is newer than the graph.
4. Persist a JSON snapshot in `SystemGraphSnapshots` after each build.

Full rebuild still occurs on first build or when incremental mode is disabled.

---

## Paginated system map API

```
GET /api/system-map/{applicationId}/paged?depth=4&page=1&pageSize=200&root=repo:{id}
```

Response includes `totalNodeCount`, `page`, `pageSize`, `maxDepth`, and `truncated` when results are capped.

The legacy `GET /api/system-map/{applicationId}` endpoint returns the full map (unchanged).

---

## Documentation export cache

Exports (`ExportDocumentationCommand`) store results in `DocumentationExportCaches` keyed by application.

Cache hits require:

- `DocumentationCacheEnabled=true`
- Entry not expired (`DocumentationCacheTtlMinutes`)
- Source fingerprint matches current repo index + schema scan + graph timestamps

---

## Diff-only schema drift scans

Manual drift detection (`DetectSchemaDriftCommand`) and the Hangfire job `schema-drift-scan` (every 6 hours) call `DetectDriftIfStaleAsync`.

When sources are unchanged, existing findings are returned without re-scanning or sending notifications.

`DatabaseConnection.LastDriftScanAt` is updated after each full drift run.

---

## Production checklist

- [ ] Enable incremental graph for portfolios with 10+ repositories
- [ ] Use `/paged` API for System Map UI on large graphs
- [ ] Set documentation cache TTL aligned with index schedule (e.g. 60 min)
- [ ] Confirm `schema-drift-scan` recurring job is registered (Hangfire Worker)
- [ ] Monitor skipped vs scanned counts in Worker logs

---

*See also: [DATA_SCALING.md](DATA_SCALING.md), [ROADMAP.md](ROADMAP.md) Horizon 3.3*

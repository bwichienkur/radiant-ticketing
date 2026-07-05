# High Availability Reference Architecture

Single-tenant EnhancementHub deployment supporting **2+ API**, **2+ Worker**, PostgreSQL HA, S3-compatible storage, and Qdrant.

---

## Topology

```
                    ┌─────────────┐
                    │  Ingress /  │
                    │  Load Bal.  │
                    └──────┬──────┘
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
      ┌─────────┐    ┌─────────┐    ┌─────────┐
      │ Web x2  │    │ API x2  │    │Worker x2│
      └────┬────┘    └────┬────┘    └────┬────┘
           │              │              │
           └──────────────┼──────────────┘
                          ▼
              ┌───────────────────────┐
              │ PostgreSQL (primary + │
              │ optional read replica)│
              └───────────────────────┘
           ┌──────────┬──────────┬──────────┐
           ▼          ▼          ▼          ▼
      Azure Blob  S3/Qdrant  OTel Coll.  Entra ID
      (DP keys)   (vectors)  (metrics)
```

---

## Component requirements

| Component | HA pattern | EnhancementHub config |
|-----------|------------|------------------------|
| **API** | 2+ replicas behind load balancer | Stateless; shared DB + key ring |
| **Web** | 2+ replicas | SignalR sticky sessions optional |
| **Worker** | 2+ replicas | Hangfire coordinates jobs; use `indexing` queue sharding |
| **PostgreSQL** | Primary + replica (Patroni, Azure Flexible Server HA) | `ConnectionStrings:Default` + `Reporting` |
| **Data Protection** | Shared key ring | `StorageProvider=AzureBlob` or NFS `KeysPath` |
| **Attachments** | S3-compatible object storage | `Storage:Provider=S3` |
| **Vector search** | Qdrant cluster or Azure AI Search | `VectorSearch:Provider=Qdrant` |
| **Observability** | OTel Collector → Grafana/Datadog | `Observability:Enabled=true` |

---

## Shared Data Protection key ring

Multi-instance API and Web **must** share the same key ring or encrypted secrets and cookies fail after restart or route to another node.

### Option A — Azure Blob (recommended on Azure)

```json
"DataProtection": {
  "ApplicationName": "EnhancementHub",
  "StorageProvider": "AzureBlob",
  "AzureBlob": {
    "ConnectionString": "<storage-account-connection-string>",
    "ContainerName": "dataprotection",
    "BlobName": "keys.xml"
  }
}
```

### Option B — NFS / Azure Files mount

```json
"DataProtection": {
  "StorageProvider": "FileSystem",
  "KeysPath": "/mnt/shared/dataprotection"
}
```

Mount the same volume on all API and Web pods/instances.

---

## Worker scaling notes

- Enable Hangfire: `BackgroundJobs:Provider=Hangfire`
- Both workers register recurring jobs; Hangfire prevents duplicate execution per job id
- Per-repository indexing uses the `indexing` queue — scale workers horizontally
- Set `DatabaseScaling:SchemaScanMaxConcurrency` to limit DB load

---

## Kubernetes deployment

Reference Helm chart: `deploy/helm/enhancementhub/`

```bash
helm upgrade --install enhancementhub deploy/helm/enhancementhub \
  -f deploy/helm/enhancementhub/values-ha.yaml \
  --set image.tag=latest \
  --set postgresql.enabled=false \
  --set externalDatabase.host=postgres-primary.internal
```

See `values.yaml` for replica counts, resource limits, and config maps.

---

## Health checks

| Endpoint | Use |
|----------|-----|
| `GET /health` | Liveness |
| `GET /health/ready` | Readiness (includes DB) |
| `GET /metrics` | Prometheus scrape (when OTel enabled) |

Configure Kubernetes probes against `/health/ready`.

---

## Production checklist

- [ ] 2+ API and Worker replicas
- [ ] Shared Data Protection (Azure Blob or NFS)
- [ ] PostgreSQL HA with automated failover tested
- [ ] Read replica for reporting connection
- [ ] S3 for attachments; Qdrant for vectors at scale
- [ ] OpenTelemetry exporting to Grafana or Datadog
- [ ] Load test: 200 repos, 500 users, 50 AI analyses/hour (Horizon 3 exit criteria)

---

*See [OBSERVABILITY.md](OBSERVABILITY.md), [DATA_SCALING.md](DATA_SCALING.md), [DEPLOYMENT.md](DEPLOYMENT.md).*

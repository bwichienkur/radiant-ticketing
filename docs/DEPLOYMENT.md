# EnhancementHub Deployment Guide

Production and pilot deployment reference for EnhancementHub.

---

## Architecture

```
                    ┌─────────────┐
                    │   Web UI    │  Razor Pages + SignalR
                    └──────┬──────┘
                           │
┌─────────────┐    ┌───────▼───────┐    ┌─────────────┐
│   Worker    │◄──►│  PostgreSQL   │◄──►│     API     │
│ (background)│    │  (+ pgvector) │    │   (REST)    │
└─────────────┘    └───────┬───────┘    └─────────────┘
                           │
              ┌────────────┼────────────┐
              ▼            ▼            ▼
           S3/local    Qdrant/Azure   OpenAI / Azure OpenAI
```

**Important:** Background jobs (indexing, AI analysis, discovery, schema scans) run in **Worker only**. Do not register background jobs in the Web host.

---

## Quick start (Docker Compose)

```bash
export OPENAI_API_KEY=sk-...   # optional; mock analysis without it
docker compose up --build
```

| Service | URL | Health |
|---------|-----|--------|
| API | http://localhost:5075/swagger | http://localhost:5075/health/ready |
| Web | http://localhost:5001 | http://localhost:5001/health/ready |
| Worker | http://localhost:5076/health/ready | background jobs |
| PostgreSQL | localhost:5432 | — |

Post-deploy smoke check:

```bash
chmod +x scripts/smoke-check.sh
./scripts/smoke-check.sh
```

---

## Production checklist

### Required configuration

| Setting | Description |
|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings:Default` | PostgreSQL connection string |
| `Database:Provider` | `PostgreSQL` |
| `Jwt:Secret` | Strong secret, ≥32 chars, not dev default |
| `DataProtection:KeysPath` | Shared filesystem path for key ring (required in Production) |
| `VectorSearch:Provider` | `PgVector`, `Qdrant`, or `AzureSearch` (not `InMemory`) |
| `Storage:Provider` | `S3` for multi-instance deployments |
| `Cors:WebOrigin` | Public Web UI origin |

### Security

- [ ] Change default admin password after first login
- [ ] Enable OpenID Connect (`Authentication:OpenIdConnect:Enabled=true`)
- [ ] Configure Entra ID group → role mappings
- [ ] Set `DataProtection:KeysPath` on shared storage for API + Web instances
- [ ] Store on-prem agent API keys in a secrets manager
- [ ] Enable attachment ClamAV scanning if required by policy
- [ ] Restrict network access to Worker (health port only if exposed)

### Services to run

| Process | Role |
|---------|------|
| `EnhancementHub.Api` | REST API, auth, uploads |
| `EnhancementHub.Web` | UI, SignalR notifications |
| `EnhancementHub.Worker` | Indexing, AI, discovery, schema scans |
| `EnhancementHub.Agent` | Optional on-prem DB scanner (customer network) |

### Database migrations

Run once per deployment (API or CI job):

```bash
dotnet ef database update \
  --project src/EnhancementHub.Infrastructure \
  --startup-project src/EnhancementHub.Api
```

---

## Environment variables (Docker/K8s)

Double-underscore nesting maps to JSON config:

```bash
Database__Provider=PostgreSQL
ConnectionStrings__Default=Host=postgres;...
Jwt__Secret=<strong-secret>
DataProtection__KeysPath=/data/dataprotection
VectorSearch__Provider=PgVector
Storage__Provider=S3
Storage__S3__Bucket=enhancementhub-uploads
OpenAI__ApiKey=sk-...
Cors__WebOrigin=https://enhancementhub.example.com
```

---

## Health checks

| Endpoint | Purpose |
|----------|---------|
| `GET /health` | Liveness (process up) |
| `GET /health/ready` | Readiness (database reachable) |

Use `/health/ready` for Kubernetes readiness probes and load balancer checks.

Example Kubernetes probe:

```yaml
readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 15
```

---

## Horizontal scaling notes

| Component | Scale | Notes |
|-----------|-------|-------|
| API | Yes | Stateless; shared DB + S3 + key ring |
| Web | Yes | Shared DB; SignalR requires backplane for multi-instance |
| Worker | Yes | Use `BackgroundJobs:Provider=Hangfire` with PostgreSQL for safe multi-worker |
| PostgreSQL | Vertical + replicas | Use read replica for reporting at scale |
| Vector search | Offload | Prefer Qdrant/Azure Search over PgVector at 100+ repos |

### Background jobs

Set `BackgroundJobs:Provider` to `Hangfire` when using PostgreSQL (recommended for Docker Compose and production). SQLite local dev uses `Polling` (default) with in-process hosted services.

```json
"BackgroundJobs": {
  "Provider": "Hangfire",
  "HangfireSchema": "hangfire"
}
```

Hangfire dashboard (Development): http://localhost:5076/hangfire

---

## On-prem agent

Register via API (Admin) or onboarding wizard. Configure the agent:

```bash
Agent__ApiBaseUrl=https://api.enhancementhub.example.com
Agent__AgentId=<from-registration>
Agent__ApiKey=<from-registration-one-time>
Agent__ConnectionId=<database-connection-id>
Agent__ConnectionString=<on-prem-db>
Agent__Provider=SqlServer
dotnet run --project src/EnhancementHub.Agent
```

---

## Troubleshooting

| Symptom | Likely cause |
|---------|--------------|
| Encrypted connection strings fail after restart | `DataProtection:KeysPath` not set or not shared |
| Discovery/indexing never completes | Worker not running |
| Duplicate discovery runs | Background job registered on Web (should be Worker only) |
| 401 on agent scan upload | Missing or invalid `X-Agent-Api-Key` header |
| 404 on applications/requests | User not in owning team (resource scoping) |

---

See also: [ROADMAP.md](ROADMAP.md), [PHASES.md](PHASES.md)

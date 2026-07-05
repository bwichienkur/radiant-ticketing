# EnhancementHub

**Turn business enhancement requests into approval-ready technical change packages—grounded in your actual code and database schema, with full audit trail.**

EnhancementHub is an AI-powered platform for enterprise IT teams that bridges the gap between business intake and technical delivery. It combines governed enhancement ticketing, repository-aware AI impact analysis, and System Intelligence (live schema mapping, drift detection, and architecture graphs) in a single .NET 8 platform.

**Ideal for:** mid-market and enterprise organizations with .NET/Azure application estates, portfolio governance requirements, and slow business-to-technical handoffs.

---

## Why EnhancementHub

| Problem | EnhancementHub outcome |
|---------|------------------------|
| Vague business requests | Structured intake with AI-generated technical scope |
| Architects spend hours on impact analysis | Repository + schema-aware analysis in minutes |
| Code and database drift undetected | Live schema scan vs indexed EF mappings |
| No audit trail for AI recommendations | Human approval gate before export to Jira/Azure DevOps |
| Air-gapped or enterprise Git | On-prem agent, ZIP upload, GitHub App clone |

---

## Architecture

Clean architecture with .NET 8:

```
src/
├── EnhancementHub.Domain/          # Entities, enums, domain rules
├── EnhancementHub.Application/     # CQRS commands/queries (MediatR), validators
├── EnhancementHub.Infrastructure/  # EF Core, AI, indexing, integrations
├── EnhancementHub.Api/             # REST API
├── EnhancementHub.Web/             # Razor Pages enterprise UI
├── EnhancementHub.Worker/          # Background jobs (indexing, AI, refresh)
└── EnhancementHub.Agent/           # On-prem DB schema scan agent

tests/
└── EnhancementHub.Tests/           # Unit + integration tests
```

**Deploy API + Web + Worker** for production. Background jobs run in Worker only.

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for production checklist, [docs/HA_ARCHITECTURE.md](docs/HA_ARCHITECTURE.md) for multi-instance HA, [docs/OBSERVABILITY.md](docs/OBSERVABILITY.md) for OpenTelemetry, [docs/DATA_SCALING.md](docs/DATA_SCALING.md) for vector offload and read replicas, [docs/SYSTEM_INTELLIGENCE_PERFORMANCE.md](docs/SYSTEM_INTELLIGENCE_PERFORMANCE.md) for graph and drift optimizations, [docs/SECURITY.md](docs/SECURITY.md) for the security whitepaper, [docs/ROADMAP.md](docs/ROADMAP.md) for the product roadmap, [docs/PRODUCT_SCORECARD.md](docs/PRODUCT_SCORECARD.md) for maturity scores, and [docs/ICP_ONE_PAGER.md](docs/ICP_ONE_PAGER.md) / [docs/DEMO_SCRIPT.md](docs/DEMO_SCRIPT.md) for sales and pilot enablement.

---

## Core capabilities

1. **Enhancement request intake** — submit, track, and manage requests across statuses
2. **Repository knowledge system** — scan Git repos, build application profiles, re-index on schedule
3. **Hybrid search** — keyword + vector search over indexed artifacts with metadata filters
4. **AI analysis workflow** — classify, retrieve context, generate impact analysis and change requests
5. **Human approval workflow** — review, edit, approve/reject with full audit trail
6. **Ticket export** — GitHub Issues, Azure DevOps, Jira (provider abstraction)
7. **Reporting** — dashboards for status, risk, approval time, AI confidence trends
8. **System Intelligence** — database schema scanning, code↔DB knowledge graph, drift detection, documentation export, refactor blast-radius analysis

---

## System Intelligence

Architecture intelligence module for connecting live databases and Git repositories:

| Capability | Description |
|------------|-------------|
| DB schema scanner | Read-only scan of SQL Server, PostgreSQL, and SQLite |
| EF entity mapping | Roslyn analysis of `[Table]`, `DbSet<>`, and fluent API mappings |
| System map | Interactive graph linking controllers, entities, tables, and APIs |
| Schema drift | Compare live DB schema against indexed code mappings |
| Documentation export | Markdown + Mermaid ERD generation |
| Refactor analysis | Blast-radius impact analysis and AI migration plans |
| On-prem agent | `EnhancementHub.Agent` console app for air-gapped DB scanning |

### On-prem agent

```bash
dotnet run --project src/EnhancementHub.Agent
```

Configure `Agent:ApiBaseUrl`, `Agent:AgentId`, `Agent:ApiKey`, `Agent:ConnectionId`, `Agent:ConnectionString`, and `Agent:Provider`.

---

## Quick start (local)

### Prerequisites

- .NET 8 SDK
- Optional: Docker for PostgreSQL + pgvector

### SQLite (default)

```bash
dotnet restore
dotnet ef database update --project src/EnhancementHub.Infrastructure --startup-project src/EnhancementHub.Api
dotnet run --project src/EnhancementHub.Api
dotnet run --project src/EnhancementHub.Web
dotnet run --project src/EnhancementHub.Worker
```

| Service | URL |
|---------|-----|
| API + Swagger | http://localhost:5075/swagger |
| Web UI | http://localhost:5001 |
| Health (API) | http://localhost:5075/health/ready |

**Dev login:** `admin@enhancementhub.dev` / `password123`

### Docker Compose (PostgreSQL + pgvector)

```bash
docker compose up --build
./scripts/smoke-check.sh
```

Set `OPENAI_API_KEY` for live AI analysis; without it, the system uses deterministic mock analysis for development.

---

## Implementation phases

| Phase | Scope | Status |
|-------|-------|--------|
| 1–9 | Core platform, System Intelligence, production scale | Complete |
| 10 | Qdrant/Azure Search, S3, email/Teams notifications | Complete |
| 11 | Onboarding wizard | Complete |
| 12–13 | Git clone, on-prem agent, attachment scanning | Complete |
| 14 | ZIP upload, GitHub App enterprise repo access | Complete |
| 15 | Enterprise hardening (PBKDF2, agent auth, resource scoping, rate limits) | Complete |
| 16+ | See [docs/ROADMAP.md](docs/ROADMAP.md) | Planned |

See [docs/PHASES.md](docs/PHASES.md) for detailed phase breakdown.

---

## Configuration

Key settings in `appsettings.json`:

```json
{
  "Database": { "Provider": "Sqlite" },
  "ConnectionStrings": { "Default": "Data Source=enhancementhub.db" },
  "Jwt": { "Secret": "...", "Issuer": "EnhancementHub", "Audience": "EnhancementHub" },
  "DataProtection": { "ApplicationName": "EnhancementHub", "KeysPath": "" },
  "OpenAI": { "ApiKey": "", "Model": "gpt-4o-mini" },
  "VectorSearch": { "Provider": "InMemory|PgVector|Qdrant|AzureSearch" },
  "Storage": { "Provider": "Local|S3" },
  "Indexing": {
    "IncrementalEnabled": true,
    "MaxFilesPerRun": 5000,
    "FreshnessSlaHours": 24,
    "ShardJobsPerRepository": true
  },
  "DatabaseScaling": { "MaxPoolSize": 100, "SchemaScanMaxConcurrency": 2 },
  "ConnectionStrings": { "Default": "...", "Reporting": "..." }
}
```

---

## Security

- JWT authentication with role-based authorization (`Admin`, `Submitter`, `Reviewer`, `Approver`, `Developer`)
- PBKDF2 password hashing; production JWT and Data Protection key validation
- Team-scoped resource access for enhancement requests and applications
- On-prem agent API key authentication (`X-Agent-Api-Key`)
- Service-to-service API keys for integrations (`X-Api-Key`, managed at `/Admin/ApiKeys`)
- Rate limiting on login and attachment upload
- Audit logging for approval and sensitive actions
- Prompt sanitization for AI inputs; human approval required before ticket export

---

## Testing

```bash
dotnet test
```

235 tests covering React onboarding advanced flows, API v1 versioning, load-test harness, accessibility CI, React SignalR collaboration, Cytoscape system map graph, React approval queue and onboarding wizard, React SPA migration, schema-per-tenant isolation, Stripe billing, UX overhaul, commercial platform, product differentiation, polyglot integrations, HA observability, System Intelligence performance, data layer scaling, indexing scale, incremental indexing, service API keys, team membership, SOC 2 readiness, data retention, onboarding wizard, audit export, enterprise AI, SSO hardening, admin operations, security, risk scoring, repository scanning, schema drift, integrations, API workflows, and role permissions.

---

## API overview

- `GET /health`, `GET /health/ready`
- `POST /api/auth/login`
- `GET/POST /api/enhancement-requests`
- `POST /api/analysis/{requestId}/trigger`
- `GET /api/system-map/{applicationId}`
- `POST /api/on-prem-agent/register`
- `GET /api/reporting/dashboard`
- `POST /api/integrations/openapi` — register OpenAPI specs
- `POST /api/integrations/polyglot/symbols` — ingest external language symbols
- `POST /api/integrations/slack/intake`, `POST /api/integrations/teams/intake` — chat intake
- `POST /api/webhooks/github` — GitHub push → auto re-index
- `GET /api/reporting/roi` — ROI metrics (Admin)
- `GET /api/templates` — enhancement domain templates
- `GET /api/analysis/{requestId}/compare` — AI vs architect comparison
- `POST /api/tenants/register` — self-service trial signup (anonymous)
- `GET /api/tenants/current/billing` — plan limits and usage metering
- `GET /api/tenants` — list tenants (Admin)

See [docs/INTEGRATIONS.md](docs/INTEGRATIONS.md), [docs/PRODUCT_DIFFERENTIATION.md](docs/PRODUCT_DIFFERENTIATION.md), and [docs/COMMERCIAL_PLATFORM.md](docs/COMMERCIAL_PLATFORM.md).

Full Swagger available at `/swagger` in Development.

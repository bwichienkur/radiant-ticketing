# EnhancementHub

AI-powered enhancement ticketing and technical impact analysis platform for enterprise software ecosystems.

EnhancementHub centralizes enhancement requests across applications and repositories, indexes codebase knowledge, runs AI impact analysis, and produces approval-ready technical change requests with full auditability.

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

## Core capabilities

1. **Enhancement request intake** — submit, track, and manage requests across statuses
2. **Repository knowledge system** — scan Git repos, build application profiles, re-index on schedule
3. **Hybrid search** — keyword + vector search over indexed artifacts with metadata filters
4. **AI analysis workflow** — classify, retrieve context, generate impact analysis and change requests
5. **Human approval workflow** — review, edit, approve/reject with full audit trail
6. **Ticket export** — GitHub Issues, Azure DevOps, Jira (provider abstraction)
7. **Reporting** — dashboards for status, risk, approval time, AI confidence trends
8. **System Intelligence** — database schema scanning, code↔DB knowledge graph, drift detection, documentation export, refactor blast-radius analysis

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

### System Intelligence API routes

- `GET/POST /api/database-connections`
- `POST /api/database-connections/{id}/scan`
- `GET /api/system-map/{applicationId}`
- `GET/POST /api/schema-drift/{connectionId}`
- `GET /api/documentation/{applicationId}/export`
- `POST /api/refactor/analyze`, `POST /api/refactor/plans`
- `POST /api/on-prem-agent/register`, `POST /api/on-prem-agent/{agentId}/scan-results`

### On-prem agent

```bash
dotnet run --project src/EnhancementHub.Agent
```

Configure `Agent:ApiBaseUrl`, `Agent:AgentId`, `Agent:ConnectionId`, `Agent:ConnectionString`, and `Agent:Provider` in `src/EnhancementHub.Agent/appsettings.json` or via `ENHANCEMENTHUB_` environment variables.

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

**Dev login:** `admin@enhancementhub.dev` / `password123`

### Docker Compose (PostgreSQL + pgvector)

```bash
docker compose up --build
```

Set `OPENAI_API_KEY` for live AI analysis; without it, the system uses deterministic mock analysis for development.

## Implementation phases

| Phase | Scope | Status |
|-------|-------|--------|
| 1 | Domain model, DB schema, enhancement CRUD, basic UI, approval workflow | Complete |
| 2 | Repository indexing, application profiles, searchable knowledge base | Complete |
| 3 | AI analysis pipeline, structured output validation, risk scoring, review UI | Complete |
| 4 | External ticket integrations, reporting, admin settings, tests | Complete |
| 5 | DB schema scanner, EF mapping, system graph, System Map UI | Complete |
| 6 | Multi-repo graph, schema drift detection, documentation export | Complete |
| 7 | Refactor blast-radius analysis, AI migration plans | Complete |
| 8 | On-prem agent, SSO stubs (OpenID Connect), System Intelligence tests | Complete |
| 9 | Column-level drift, pgvector search, SSO role mapping, notifications, blob storage | Complete |
| 10 | Qdrant/Azure Search vectors, S3 presigned URLs, email/Teams notifications | Complete |

See [docs/PHASES.md](docs/PHASES.md) for detailed phase breakdown.

## Configuration

Key settings in `appsettings.json`:

```json
{
  "Database": { "Provider": "Sqlite" },
  "ConnectionStrings": { "Default": "Data Source=enhancementhub.db" },
  "Jwt": { "Secret": "...", "Issuer": "EnhancementHub", "Audience": "EnhancementHub" },
  "OpenAI": { "ApiKey": "", "Model": "gpt-4o-mini", "Endpoint": "https://api.openai.com/v1" },
  "VectorSearch": { "Provider": "InMemory|PgVector|Qdrant|AzureSearch", "Dimensions": 64 },
  "Storage": { "Provider": "Local|S3", "LocalRoot": "uploads" },
  "Notifications": {
    "Email": { "Enabled": false, "SmtpHost": "", "ToAddresses": [] },
    "Teams": { "Enabled": false, "WebhookUrl": "" }
  },
}
```

## Security

- JWT authentication with role-based authorization (`Admin`, `Submitter`, `Reviewer`, `Approver`, `Developer`)
- Audit logging for all approval and sensitive actions
- Prompt sanitization for AI inputs
- AI cannot modify code or export tickets without human approval

## Testing

```bash
dotnet test
```

56 tests covering risk scoring, AI validation, repository scanning, EF entity mapping, schema drift detection, documentation export, enterprise integrations, API integration, approval workflow, role permissions, and ticket export.

## API overview

- `POST /api/auth/login`
- `GET/POST /api/enhancement-requests`
- `POST /api/enhancementrequests/{id}/attachments`
- `GET /api/enhancementrequests/{id}/attachments/{attachmentId}/download`
- `POST /api/approvals/{id}/actions`
- `POST /api/repositories`, `POST /api/repositories/{id}/index`
- `GET /api/knowledge/search`
- `POST /api/analysis/{requestId}/trigger`
- `POST /api/external-tickets/export`
- `GET /api/reporting/dashboard`

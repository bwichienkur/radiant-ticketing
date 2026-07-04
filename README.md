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
└── EnhancementHub.Worker/        # Background jobs (indexing, AI, refresh)

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

See [docs/PHASES.md](docs/PHASES.md) for detailed phase breakdown.

## Configuration

Key settings in `appsettings.json`:

```json
{
  "Database": { "Provider": "Sqlite" },
  "ConnectionStrings": { "Default": "Data Source=enhancementhub.db" },
  "Jwt": { "Secret": "...", "Issuer": "EnhancementHub", "Audience": "EnhancementHub" },
  "OpenAI": { "ApiKey": "", "Model": "gpt-4o-mini", "Endpoint": "https://api.openai.com/v1" }
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

40 tests covering risk scoring, AI validation, repository scanning, API integration, approval workflow, role permissions, and ticket export.

## API overview

- `POST /api/auth/login`
- `GET/POST /api/enhancement-requests`
- `POST /api/approvals/{id}/actions`
- `POST /api/repositories`, `POST /api/repositories/{id}/index`
- `GET /api/knowledge/search`
- `POST /api/analysis/{requestId}/trigger`
- `POST /api/external-tickets/export`
- `GET /api/reporting/dashboard`

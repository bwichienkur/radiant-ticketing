# EnhancementHub Implementation Phases

## Phase 1 — Domain, Database, CRUD, UI, Approval

- Full domain model with 27+ entities
- EF Core schema with migrations (SQLite/PostgreSQL)
- Enhancement request intake with all required statuses
- CQRS commands/queries via MediatR
- Razor Pages UI: dashboard, submit, detail, approval queue
- Approval workflow with audit logging

## Phase 2 — Repository Indexing & Knowledge Base

- Git repository registration and branch tracking
- Roslyn-based C# scanner (controllers, services, DbContext, entities)
- Application profile generation
- Indexed files and symbols storage
- Hybrid keyword + vector search abstraction (`IVectorSearchService`)
- On-demand and scheduled re-indexing via Worker

## Phase 3 — AI Analysis Pipeline

- Multi-step AI workflow: classify → retrieve → analyze → generate change request
- Structured JSON output with schema validation
- Risk scoring service (Low/Medium/High/Critical)
- Confidence scores and open questions
- Analysis review UI on request detail page
- Full prompt/response audit trail (`AiPromptRun`, `RetrievedContextItem`)

## Phase 4 — Integrations, Reporting, Hardening

- External ticket export: GitHub, Azure DevOps, Jira (swappable via factory)
- Dashboard reporting (status counts, approval time, risk trends)
- Admin settings and AI prompt configuration
- Correlation IDs, Polly retries, structured logging
- Unit and integration tests
- Docker Compose for local PostgreSQL deployment

## Phase 5 — System Intelligence: Schema & Graph Foundation

- `DatabaseConnection` registration with encrypted connection strings
- Read-only schema scanners: SQL Server, PostgreSQL, SQLite
- EF entity↔table mapping via Roslyn (`[Table]`, `DbSet<>`, fluent API)
- `CodeEntityMapping` persistence during repository indexing
- System knowledge graph (`SystemGraphNode`, `SystemGraphEdge`)
- System Map UI and database connection management pages

## Phase 6 — Drift Detection & Documentation

- Multi-repository graph builder linking code and database artifacts
- Schema drift detector comparing live DB vs indexed EF mappings
- Drift report UI with severity classification
- Markdown + Mermaid ERD documentation export
- ERD visualization page per database connection

## Phase 7 — Refactor Intelligence

- Blast-radius analysis across controllers, entities, and tables
- AI-assisted refactor plan generation with migration steps
- Refactor plan storage and review UI

## Phase 8 — Enterprise Deployment

- On-prem agent (`EnhancementHub.Agent`) for air-gapped database scanning
- Agent scan result ingestion via `POST /api/on-prem-agent/{agentId}/scan-results`
- OpenID Connect SSO stub (enable via `Authentication:OpenIdConnect:Enabled`)
- System Intelligence unit tests (EF mapping, drift detection, doc export)

## Phase 9 — Production Scale & Enterprise Readiness

- Column-level schema drift detection (type, nullability, missing columns)
- `CodeEntityProperty` extraction via Roslyn during repository indexing
- pgvector semantic search provider for PostgreSQL (`VectorSearch:Provider=PgVector`)
- Azure AD / Entra ID SSO role mapping from security groups
- Real-time SignalR notifications for scans, indexing, and drift detection
- Blob storage abstraction with local disk and S3-compatible stub
- Enhancement request attachment upload API

## Future enhancements

- Azure AI Search / Qdrant vector provider implementations
- Full S3 attachment storage with presigned URLs
- Real-time email/Teams notification channels
- Attachment virus scanning

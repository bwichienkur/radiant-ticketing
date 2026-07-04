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
- 40 unit and integration tests
- Docker Compose for local PostgreSQL deployment

## Future enhancements

- pgvector production deployment for semantic search at scale
- Azure AI Search / Qdrant vector provider implementations
- SSO (Azure AD / OIDC)
- Real-time notifications
- Attachment blob storage (S3/Azure Blob)

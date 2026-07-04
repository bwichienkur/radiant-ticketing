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

## Phase 10 — Enterprise Integrations

- Qdrant vector search provider (`VectorSearch:Provider=Qdrant`)
- Azure AI Search vector provider (`VectorSearch:Provider=AzureSearch`)
- Full S3 attachment storage with presigned download URLs
- Attachment download API (`GET /api/enhancementrequests/{id}/attachments/{attachmentId}/download`)
- Email notification channel via SMTP (`Notifications:Email`)
- Microsoft Teams webhook notification channel (`Notifications:Teams`)
- Composite notification publisher fan-out across enabled channels

## Phase 11 — Client Onboarding Wizard

- Guided 6-step application setup wizard (`/Onboarding/Wizard`)
- Application creation UI with auto team provisioning
- Repository path validation with Roslyn preview stats
- Database registration or skip path inside wizard flow
- One-click discovery orchestration (index, scan, graph, drift)
- Review screen with documentation export and system map links
- Dashboard getting-started checklist with resume support
- Empty states on Applications, Repositories, and Database Connections pages

## Phase 12 — Onboarding Polish & Async Discovery

- Server-side Git clone during onboarding (`Clone from Git` tab)
- Connection string builder for database registration
- On-prem agent setup tab with copy-paste agent configuration
- Background discovery job with queued/running/completed states
- Wizard auto-refresh while discovery runs

## Phase 13 — Attachment Security

- `IAttachmentScanService` with extension whitelist and magic-byte validation
- Optional ClamAV INSTREAM virus scanning (`Attachments:Scanning:ClamAv`)
- Attachment scan status persisted on `EnhancementAttachment`
- Upload rejected when scan fails

## Phase 14 — Air-Gapped & Enterprise Repo Access

- ZIP archive upload for air-gapped repository onboarding (wizard tab)
- Zip-slip protection and source file validation on extract
- GitHub App integration for enterprise private repo cloning
- Installation token flow with JWT app authentication
- GitHub App status detection in onboarding wizard

## Phase 15 — Enterprise Hardening (Tier 1)

- PBKDF2 password hashing replaces development SHA256 hasher
- Data Protection keys persisted to filesystem (`DataProtection:KeysPath`)
- Production startup validation for JWT secret and key persistence
- On-prem agent API key authentication on `scan-results` (`X-Agent-Api-Key`)
- DB-backed on-prem agent registry with hashed API keys
- Resource-level authorization for enhancement requests and attachments
- Rate limiting on login and attachment upload endpoints

## Horizon 1 — Pilot Readiness (complete)

- Background jobs run in Worker only (removed duplicate discovery job from Web)
- Health checks on API, Web, and Worker (`/health`, `/health/ready`)
- Production deployment guide (`docs/DEPLOYMENT.md`) and smoke check script
- README repositioned around business outcomes

## Phase 16 — System Intelligence authorization (complete)

- Team-scoped access on all System Intelligence commands and read queries
- Application, connection, drift report, and refactor plan scoping via `IApplicationAccessService`

## Phase 18 — Durable job orchestration (complete)

- Hangfire + PostgreSQL storage when `BackgroundJobs:Provider=Hangfire`
- Shared job executors used by Hangfire recurring jobs and polling fallback
- Hangfire dashboard at `/hangfire` on Worker (Development)
- Docker Compose Worker uses Hangfire by default

## Phase 17 — Enterprise AI (complete)

- Unified `IChatCompletionService` supporting OpenAI and Azure OpenAI
- Per-workflow model/deployment config (`EnhancementAnalysis`, `RefactorPlan`)
- Token and cost tracking on `AiPromptRun` with daily budget enforcement
- PII redaction (email, phone, SSN, card patterns) before prompts
- Admin AI usage report (`GET /api/admin/ai-usage`)
- Rate limiting on manual AI analysis triggers (5/min)

## Future enhancements

- GitHub App webhook-driven automatic re-index on push
- Support for monorepo subdirectory selection during onboarding

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

## Phase 16 continuation — Entra ID SSO & admin operations

- Production validation for OpenID Connect settings when SSO is enabled
- Entra ID claim hardening (`oid`, `roles`, `groups`, email, display name)
- Entra ID setup guide (`docs/ENTRA_ID_SSO.md`)
- Admin background job status API (`GET /api/admin/jobs/status`)
- Hangfire monitoring storage on API (no job server) for queue statistics
- Dashboard widgets: awaiting analysis, high-risk pending approval
- Request detail: AI analysis summary banner above the fold with risk badge

## Phase 18 continuation — Admin operations UI

- Admin background jobs page (`/Admin/Jobs`) with queue depth, schedules, failed jobs, and Hangfire retry
- Admin authentication page (`/Admin/Authentication`) with OIDC config and role mapping validation
- `GET /api/admin/authentication/status` and `POST /api/admin/jobs/{jobId}/retry`
- System Map UX: empty states, loading indicator, type-grouped tabs

## Horizon 2 continuation — Audit log export

- CSV and JSON export with date range, entity type, and action filters
- Admin-only export via `GET /api/auditlogs/export` and Audit page buttons
- Export includes previous/new values and correlation IDs for compliance review

## Horizon 1 continuation — Onboarding wizard polish

- Resume banner and clickable progress steps for completed wizard stages
- Form prefill from registered application, repository, and database connection
- Persisted wizard errors (`WizardError`) with recovery on step advance
- Discovery failure panel with retry guidance and links back to code/database steps

## Horizon 2 continuation — Data retention

- Configurable retention for `AiPromptRun` records and enhancement attachments (`Retention` config section)
- Daily background job when `Retention:Enabled=true` (Hangfire or polling Worker)
- Admin page `/Admin/Retention` with preview and manual apply
- API: `GET /api/admin/retention/status`, `POST /api/admin/retention/apply?dryRun=true|false`

## Horizon 2 continuation — Compliance documentation

- SOC 2 control → feature mapping (`docs/SOC2_READINESS.md`)
- Security whitepaper for procurement reviews (`docs/SECURITY.md`)
- Admin compliance dashboard (`/Admin/Compliance`) with configuration-aware status
- API: `GET /api/admin/compliance/soc2`

## Horizon 2 continuation — Team membership management

- Admin teams list and detail pages (`/Admin/Teams`, `/Admin/Teams/{id}`)
- Invite users by email (creates account with temporary password or links existing user)
- Update team roles (Owner, Lead, Member) and remove members
- Audit logging for membership changes
- API: `GET/POST /api/admin/teams`, `GET /api/admin/teams/{id}`, member CRUD endpoints

## Horizon 1 continuation — Positioning & packaging

- ICP one-pager for sales and design partner qualification (`docs/ICP_ONE_PAGER.md`)
- 10-minute live demo script with talk track and Q&A (`docs/DEMO_SCRIPT.md`)
- Draft pricing: design partner pilot, Team, and Enterprise tiers (`docs/PRICING.md`)

## Horizon 2 continuation — Service API keys

- Machine-to-machine authentication via `X-Api-Key` header (`eh_` prefix)
- Policy scheme accepts JWT bearer tokens or API keys on the API
- Admin page `/Admin/ApiKeys` to create, list, and revoke keys
- Each key maps to a dedicated service user with configurable role and optional team scope
- Hashed key storage; plain key shown once at creation
- API: `GET/POST /api/admin/api-keys`, `DELETE /api/admin/api-keys/{id}`

## Phase 19 — Incremental indexing at scale (complete)

- Git diff-based incremental repository indexing when `Indexing:IncrementalEnabled=true`
- Tracks `RepositoryBranch.LastCommitHash` after each index run
- Processes only changed/deleted files; full reindex when prior commit is missing or diff fails
- Configurable via `Indexing:MaxFilesPerRun` (default 5000) and `Indexing:FreshnessSlaHours` (default 24)
- Repository status API exposes last commit hash and incremental flag
- Monorepo subdirectory scoping via `Repository.SourceSubdirectory` and priority via `Repository.IndexingPriority`
- Hangfire per-repository indexing jobs on dedicated `indexing` queue when `Indexing:ShardJobsPerRepository=true`
- Admin index freshness report: `GET /api/admin/indexing/freshness` and `/Admin/Jobs` dashboard

## Phase 20 — Data layer scaling (complete)

- Read replica connection for reporting via `ConnectionStrings:Reporting` and `IReportingDbContext`
- Vector search production validation with Qdrant/Azure Search recommendations (`/Admin/DataScaling`)
- Npgsql connection pool tuning via `DatabaseScaling` config section
- Parallel schema scan concurrency limit (`SchemaScanMaxConcurrency`)
- AI prompt run JSON archival before retention purge (`Retention:ArchiveAiPromptRunsBeforeDelete`)
- Deployment guide: `docs/DATA_SCALING.md`

## Phase 21 — System Intelligence performance (complete)

- Incremental graph rebuild when `SystemIntelligence:IncrementalGraphEnabled=true`
- Graph snapshots persisted after each build for fast map reads
- Paginated system map API: `GET /api/system-map/{id}/paged` with depth and page limits
- Documentation export cache with fingerprint-based TTL invalidation
- Diff-only schema drift via `DetectDriftIfStaleAsync` and Hangfire `schema-drift-scan` job
- Guide: `docs/SYSTEM_INTELLIGENCE_PERFORMANCE.md`

## Phase 22 — HA & observability (complete)

- Azure Blob or NFS shared Data Protection key ring (`DataProtection:StorageProvider`)
- OpenTelemetry traces and metrics via `Observability` config (OTLP + Prometheus `/metrics`)
- Hangfire job duration metrics and trace spans
- Admin observability dashboard (`/Admin/Observability`, `GET /api/admin/observability/status`)
- Grafana dashboard and Datadog monitor templates under `deploy/observability/`
- Reference HA architecture: `docs/HA_ARCHITECTURE.md`
- Kubernetes Helm chart: `deploy/helm/enhancementhub/`
- Local observability stack: `docker-compose.observability.yml`

## Phase 23 — Polyglot & integration expansion (complete)

- OpenAPI 3 spec registration and endpoint ingestion (`OpenApiRegistration`, `OpenApiEndpoint`)
- Partner polyglot symbol ingestion API for Java, Python, TypeScript, and JavaScript
- GitHub push webhook with HMAC verification → auto-index when `Repository.AutoIndexOnPush=true`
- Slack and Teams chat intake endpoints for enhancement request submission
- ServiceNow outbound exporter and inbound webhook status sync
- Integration guide: `docs/INTEGRATIONS.md`
- API: `/api/integrations/*`, `POST /api/webhooks/github`

## Phase 24 — Product-led differentiation (complete)

- ROI dashboard (`/Admin/Roi`, `GET /api/reporting/roi`) — analysis time saved, drift resolved, architect edits
- Approval policy engine with rules by risk, department, and application tier
- `Application.Tier` field (`Standard`, `Critical`, `Low`)
- Enhancement templates for Security, Performance, and Compliance domains
- Analysis comparison API and request detail comparison view
- Architect edit recording and finding human-approval workflow
- Schema drift finding resolve endpoint
- Guide: `docs/PRODUCT_DIFFERENTIATION.md`

## Phase 25 — UX modernization (complete)

- Blazor vs React SPA evaluation with vanilla JS pilot (`/Spa/RequestDetail/{id}`, `web-api/spa/*`)
- Mobile-responsive approval queue with sticky actions and touch targets
- Real-time collaboration hub (`/hubs/request-collaboration`) on request detail
- WCAG 2.1 AA baseline: skip link, focus rings, live regions, reduced motion
- Guides: `docs/UX_MODERNIZATION.md`, `docs/ACCESSIBILITY.md`

## Phase 26 — Commercial platform (complete)

- Row-level multi-tenant isolation via `TenantId` on users and teams
- Usage metering and plan limits (`Trial`, `Team`, `Enterprise`)
- Self-service trial signup (`/Account/Signup`, `POST /api/tenants/register`)
- Regional tenant metadata (`US`, `EU`, `APAC`) for data residency routing
- Admin tenancy dashboard (`/Admin/Tenancy`) and billing API
- Guide: `docs/COMMERCIAL_PLATFORM.md`

## Phase 27 — UX overhaul (complete)

- Sidebar app shell, command palette (⌘K), notification center, persistent dark mode
- Dashboard control room with copilot bar, activity feed, and sparkline trends
- Request list triage (search/filter/sort/mobile cards) and approval queue v2
- Request detail mission control, accordions, inline collaboration comments
- Template-card create flow; admin sub-nav; SPA pilot skeleton + mission control
- Guide: `docs/UX_MODERNIZATION.md` (Phase 27 section)

## Phase 28 — Stripe billing (complete)

- Stripe Checkout and Billing Portal integration (`StripeOptions`, `IStripeBillingService`)
- Tenant subscription fields (`StripeCustomerId`, `StripeSubscriptionId`, `SubscriptionStatus`)
- Billing API: `POST /api/billing/checkout`, `POST /api/billing/portal`
- Stripe webhooks: `POST /api/webhooks/stripe` for checkout and subscription lifecycle
- Trial expiry enforcement in `TenantBillingService.EnsureWithinLimitsAsync`
- Admin upgrade CTAs on `/Admin/Tenancy`
- Guide: `docs/STRIPE_BILLING.md`

## Phase 29 — Schema-per-tenant isolation (complete)

- `TenantIsolationMode` (`SharedRowLevel`, `DedicatedSchema`) on tenant records
- PostgreSQL schema provisioner clones tenant tables into `tenant_{slug}` schemas
- `TenantSearchPathConnectionInterceptor` + `TenantIsolationMiddleware` for per-request routing
- APIs: `GET/POST /api/tenants/current/isolation` (+ provision)
- Auto-provision on Enterprise upgrade; admin provision CTA on `/Admin/Tenancy`
- Guide: `docs/TENANT_ISOLATION.md`

## Phase 30 — React SPA migration (complete)

- React + Vite + TypeScript `ClientApp` with MSBuild integration
- Migrated hot paths: `/Spa/RequestDetail/{id}`, `/Spa/SystemMap`
- BFF endpoints: `web-api/spa/applications`, `web-api/spa/system-map/{id}`
- Shared React components: mission control, loading skeleton
- Built bundles published to `wwwroot/spa/react/`
- Guide: `docs/UX_MODERNIZATION.md` (Phase 30 section)

## Phase 31 — React approval & onboarding (complete)

- React approval queue at `/Spa/ApprovalQueue` with J/K navigation and quick actions
- React onboarding wizard at `/Spa/OnboardingWizard` with step progress and discovery polling
- BFF endpoints for approvals and onboarding under `web-api/spa/*`
- Classic Razor views retain links to React alternatives
- Guide: `docs/UX_MODERNIZATION.md` (Phase 31 section)

## Future enhancements

- Graph visualization library (Cytoscape/D3) for React system map

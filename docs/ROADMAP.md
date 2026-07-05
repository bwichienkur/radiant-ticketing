# EnhancementHub Product Roadmap

Strategic roadmap derived from product, marketability, and scalability evaluation (July 2026).
Organized into four horizons: **Now**, **Next**, **Scale**, and **Grow**.

Track scores over time in [PRODUCT_SCORECARD.md](PRODUCT_SCORECARD.md).

---

## Vision

**Turn business enhancement requests into approval-ready technical change packages—grounded in actual code and database schema, with full audit trail.**

Primary ICP: mid-market and enterprise organizations with **.NET/Azure application estates**, governance requirements, and weak linkage between business intake and technical design.

---

## Current state (Phase 15 baseline)

| Area | Status |
|------|--------|
| Core intake + approval + audit | Complete |
| Repository indexing + AI analysis | Complete |
| System Intelligence (schema, graph, drift, docs) | Complete |
| Onboarding wizard + enterprise repo access | Complete |
| Tier 1 security hardening | Complete (Phase 15) |
| Multi-tenant SaaS | Not started |
| Durable job queue | Not started |
| Production observability | Minimal |

---

## Roadmap overview

```
Horizon 1 — Now (0–6 weeks)     Pilot readiness & demo quality
Horizon 2 — Next (6–12 weeks)  Enterprise buyer requirements
Horizon 3 — Scale (3–6 months)   Large portfolio & HA operations
Horizon 4 — Grow (6–12 months)   Market expansion & defensibility
```

---

## Horizon 1 — Now: Pilot readiness & demo quality

**Goal:** Make EnhancementHub credible for 1–2 design-partner pilots without implementation heroics.

### 1.1 Positioning & packaging
- [x] Rewrite landing/README around **outcome** (hours saved, approval-ready packages), not feature list
- [x] Define ICP one-pager: ".NET on Azure, 50–500 devs, portfolio governance pain" (`docs/ICP_ONE_PAGER.md`)
- [x] Create 10-minute demo script: intake → AI analysis → system map → approval → Jira export (`docs/DEMO_SCRIPT.md`)
- [x] Pricing/packaging draft: pilot vs enterprise license tiers (`docs/PRICING.md`)

### 1.2 Demo-critical UX polish
- [x] Enhancement request detail: analysis summary above the fold, risk badge, confidence score
- [x] System Map: default layout, loading states, empty-state guidance
- [x] Onboarding wizard: progress persistence, clearer error recovery
- [x] Dashboard: "requests awaiting analysis" and "high-risk pending approval" widgets

### 1.3 Operational fixes (quick wins)
- [x] Run `ApplicationDiscoveryJob` in **Worker only** (remove from Web to prevent duplicate work)
- [x] Consolidate background job registration behind a single `registerBackgroundJobs` flag
- [x] Add health check endpoints (`/health`, `/health/ready`) on API and Worker
- [x] Document Production deployment checklist (JWT, DataProtection keys, Postgres, S3)

### 1.4 Test & docs
- [x] Update README test count and Phase 15 security section
- [x] Add `docs/DEPLOYMENT.md` with Docker Compose + Kubernetes sketch
- [x] Add smoke test script for post-deploy validation
- [x] Begin Phase 16: `IApplicationAccessService` for System Intelligence list/get scoping

**Exit criteria:** A design partner can deploy via Docker Compose, complete onboarding, submit a request, receive AI analysis, view system map, and export to Jira—without developer assistance.

---

## Horizon 2 — Next: Enterprise buyer requirements

**Goal:** Close gaps that block enterprise procurement and security review.

### 2.1 Identity & access (Phase 16)
- [x] Azure Entra ID as documented default SSO path (`docs/ENTRA_ID_SSO.md`)
- [x] Group → role mapping validation in admin UI (`/Admin/Authentication`)
- [x] Extend resource authorization to System Intelligence APIs (applications, connections, exports, drift reports)
- [x] Team membership management UI (invite, assign roles)
- [x] Optional: API keys for service-to-service integrations

### 2.2 Enterprise AI (Phase 17)
- [x] Azure OpenAI provider alongside OpenAI
- [x] Configurable model per workflow step (classify, analyze, refactor)
- [x] Token/cost tracking per request (`AiPromptRun` aggregation dashboard)
- [x] PII redaction hook before prompt submission
- [x] Rate limits and daily budget caps per tenant/org

### 2.3 Job orchestration (Phase 18)
- [x] Replace polling `BackgroundService` jobs with durable queue (Hangfire + PostgreSQL)
- [x] Idempotent job handlers with shared executors and `DisableConcurrentExecution` via Hangfire
- [x] Job status API: indexing, discovery, schema scan, AI analysis (`GET /api/admin/jobs/status`)
- [x] Admin UI: queue depth, failed jobs, manual retry (`/Admin/Jobs`)

### 2.4 Compliance & audit
- [x] Immutable audit log export (CSV/JSON) with date range filter
- [x] Data retention policies for AI prompt runs and attachments
- [x] SOC 2 readiness checklist mapping (control → feature)
- [x] Security whitepaper: auth, encryption at rest, agent model, AI data flow

**Exit criteria:** Passes security questionnaire for a mid-size enterprise; SSO + Azure OpenAI + durable jobs operational.

---

## Horizon 3 — Scale: Large portfolio & HA operations

**Goal:** Support 100–500 repositories and 500+ users in a single-tenant deployment.

### 3.1 Indexing at scale (Phase 19)
- [x] Incremental indexing (git diff since last indexed commit)
- [x] Per-repository job sharding across Worker instances (Hangfire `indexing` queue)
- [x] Large monorepo mode: subdirectory scoping, file count limits, priority queues
- [x] Index freshness SLA metrics (% repos indexed within N hours)

### 3.2 Data layer scaling (Phase 20)
- [x] Default vector offload to Qdrant or Azure Search for large deployments
- [x] Read replica support for reporting queries
- [x] Audit log and `AiPromptRun` archival/partitioning strategy
- [x] Connection pool tuning and scan concurrency limits

### 3.3 System Intelligence performance (Phase 21)
- [x] Incremental system graph updates (not full rebuild)
- [x] Graph query pagination and depth limits
- [x] Cached documentation exports with TTL invalidation
- [x] Schema drift: scheduled diff-only scans

### 3.4 High availability & observability (Phase 22)
- [x] Shared Data Protection key ring (Azure Blob / NFS) documented and tested
- [x] OpenTelemetry traces + metrics (ASP.NET, EF, HTTP, job duration)
- [x] Structured dashboards: Grafana/Datadog templates
- [x] Reference HA architecture: 2+ API, 2+ Worker, Postgres HA, S3, Qdrant
- [x] Kubernetes Helm chart or Terraform module

**Exit criteria:** Load test: 200 repos, 500 concurrent users, 50 AI analyses/hour without job duplication or data loss.

---

## Horizon 4 — Grow: Market expansion & defensibility

**Goal:** Broaden addressable market and deepen competitive moat.

### 4.1 Polyglot & integration expansion (Phase 23)
- [x] OpenAPI/Swagger ingestion for non-.NET APIs
- [x] Java/Python tree-sitter or LSP-based symbol extraction (or partner integration)
- [x] GitHub App webhooks → automatic re-index on push
- [x] Slack / Teams intake bot (submit enhancement from chat)
- [x] ServiceNow bi-directional sync (optional)

### 4.2 Product-led differentiation (Phase 24)
- [x] ROI dashboard: analysis time saved, risk prevented, drift findings resolved
- [x] Policy engine: approval rules by risk level, department, application tier
- [x] Enhancement templates by domain (security, performance, compliance)
- [x] Comparison view: AI recommendation vs architect edits (learning signal)

### 4.3 UX modernization (Phase 25)
- [x] Evaluate Blazor/React SPA for high-traffic pages (detail, system map, wizard)
- [x] Mobile-responsive approval queue
- [x] Real-time collaboration on request detail (SignalR already present)
- [x] Accessibility (WCAG 2.1 AA) pass on core flows

### 4.4 Commercial platform (Phase 26 — optional)
- [x] Multi-tenant data isolation (schema-per-tenant or row-level security)
- [x] Billing/metering (applications, analyses, storage)
- [x] Self-service signup and trial sandbox
- [x] Regional deployment (EU data residency)

**Exit criteria:** Two published case studies; pipeline beyond .NET-only ICP; measurable ROI metrics in product.

---

## Cross-cutting engineering debt

Address incrementally across horizons; do not defer entirely.

| Item | Target horizon | Priority |
|------|----------------|----------|
| Remove EF types from Application layer (repository abstractions) | 2 | High |
| Duplicated AI logic in jobs vs commands | 2 | Medium |
| Consistent authorization on all mutating endpoints | 2 | High |
| Replace InMemory vector default with PgVector in production templates | 1 | Medium |
| Integration test coverage for auth scoping + job idempotency | 2 | High |
| API versioning strategy (`/api/v1`) | 3 | Medium |

---

## Metrics & success criteria

Track monthly from first pilot:

| Metric | Pilot target | Enterprise target |
|--------|--------------|-------------------|
| Time: request submitted → analysis complete | < 30 min | < 10 min |
| Time: analysis → approval decision | < 5 days | < 2 days |
| % requests with linked application + repo | > 80% | > 95% |
| Schema drift findings acknowledged | baseline | ↓ 25% YoY |
| Pilot NPS (architects + PMs) | > 30 | > 50 |
| Platform uptime (pilot SLA) | 99% | 99.9% |

---

## Suggested phase numbering (continues PHASES.md)

| Phase | Name | Horizon |
|-------|------|---------|
| 16 | Enterprise IAM & authorization completeness | 2 |
| 17 | Azure OpenAI & AI ops | 2 |
| 18 | Durable job orchestration | 2 |
| 19 | Incremental indexing at scale | 3 |
| 20 | Data layer & vector scaling | 3 |
| 21 | System Intelligence performance | 3 |
| 22 | HA, observability & K8s | 3 |
| 23 | Polyglot & integration expansion | 4 |
| 24 | ROI, policy engine & differentiation | 4 |
| 25 | UX modernization | 4 |
| 26 | Multi-tenant commercial platform | 4 (optional) |

---

## Recommended immediate next steps (this sprint)

1. ~~**Remove duplicate `ApplicationDiscoveryJob` from Web**~~ — done (Horizon 1)
2. ~~**Add health checks + deployment doc**~~ — done (Horizon 1)
3. ~~**Rewrite README value proposition**~~ — done (Horizon 1)
4. **Continue Phase 16** (authorization completeness + Entra ID hardening) — started with application scoping
5. **Plan Phase 18** (job queue) — unblocks scale testing

---

## What not to build yet

Avoid distracting from pilot closure:

- Multi-tenant SaaS billing (until 2+ paying self-hosted customers)
- Full ServiceNow replacement scope
- Mobile native apps
- Custom LLM fine-tuning
- Real-time collaborative editing (defer to Phase 25)

---

*Last updated: July 2026 — Phase 25 (UX modernization).*

# EnhancementHub Security Whitepaper

Technical security overview for enterprise security reviews, procurement questionnaires, and architecture boards.

**Version:** July 2026 (Phase 15–18 + compliance features)  
**Deployment model:** Single-tenant (customer-managed infrastructure)

---

## 1. Executive summary

EnhancementHub is a .NET 8 application that ingests business enhancement requests, analyzes them against indexed code and database schema, and produces approval-ready change packages with a full audit trail.

Security design principles:

- **Defense in depth** — authentication, authorization, input validation, rate limiting, optional malware scanning
- **Least privilege** — read-only database scanning by default; scoped API access; team-based System Intelligence authorization
- **Auditability** — immutable audit log, AI prompt run history, exportable compliance artifacts
- **Customer control** — self-hosted deployment; customer owns data, keys, and identity provider

---

## 2. Architecture & trust boundaries

```
┌─────────────────────────────────────────────────────────────┐
│                    Customer network / cloud                  │
│  ┌─────────┐   ┌─────────┐   ┌──────────┐   ┌───────────┐  │
│  │   Web   │   │   API   │   │  Worker  │   │ On-prem   │  │
│  │  (UI)   │   │ (REST)  │   │  (jobs)  │   │   Agent   │  │
│  └────┬────┘   └────┬────┘   └────┬─────┘   └─────┬─────┘  │
│       │             │             │               │         │
│       └─────────────┴──────┬──────┴───────────────┘         │
│                            ▼                                 │
│                     PostgreSQL / SQLite                      │
│                     Local or S3 storage                      │
└────────────────────────────┬────────────────────────────────┘
                             │ TLS (customer responsibility)
                             ▼
              OpenAI / Azure OpenAI, GitHub, Jira, SMTP, Teams
```

| Boundary | Responsibility |
|----------|----------------|
| User → Web/API | Customer IdP (Entra ID) or local credentials |
| API → Database | Connection string in customer vault; encrypted at rest in app DB |
| Worker → Repositories | Git clone or local path; customer network access |
| Agent → API | API key per agent; outbound-only from customer network |
| API → AI provider | API keys; optional Azure OpenAI for data residency |

---

## 3. Authentication

### 3.1 Web UI (Razor Pages)

- Cookie-based session (8-hour default)
- Optional **Microsoft Entra ID** via OpenID Connect (authorization code + PKCE)
- Group and app-role → EnhancementHub role mapping (`docs/ENTRA_ID_SSO.md`)

### 3.2 REST API

- JWT bearer tokens issued by `POST /api/auth/login`
- Configurable issuer, audience, and signing secret
- Production startup rejects default development secret

### 3.3 On-prem agent

- Per-agent API key transmitted in `X-Agent-Api-Key`
- Keys stored hashed (PBKDF2); plain key shown once at registration
- Agent authenticates on each scan result upload

### 3.4 Service-to-service API keys

- Machine integrations authenticate with `X-Api-Key` (prefix `eh_`)
- Each key maps to a dedicated service user with a configurable global role
- Optional team assignment scopes application access like human users
- Keys stored hashed; plain value shown once at creation in `/Admin/ApiKeys`
- Revocation disables the key and associated service account immediately

---

## 4. Authorization

| Layer | Mechanism |
|-------|-----------|
| Global roles | Admin, Approver, Developer, Reviewer, Submitter |
| Enhancement requests | Submitter/team visibility via `IEnhancementRequestAccessService` |
| System Intelligence | Application/team scoping via `IApplicationAccessService` |
| Admin operations | `[Authorize(Roles = "Admin")]` — settings, AI prompts, jobs, retention, compliance |
| Audit export | Admin-only |

---

## 5. Encryption

### 5.1 At rest

| Data | Protection |
|------|------------|
| User passwords | PBKDF2 hash (salt + hash) |
| Database connection strings | ASP.NET Data Protection encryption |
| On-prem agent API keys | Hashed; not reversible |
| Service API keys | Hashed; not reversible; prefix-only display in admin UI |
| Data Protection key ring | Filesystem path (`DataProtection:KeysPath`) — use shared storage for multi-instance |
| Attachments | Local filesystem or S3-compatible object storage |
| JWT signing key | Configuration secret (environment variable / vault) |

### 5.2 In transit

- HTTPS required in production (terminate at load balancer or ingress)
- TLS to external services (OpenAI, Azure, GitHub, PostgreSQL with SSL)

---

## 6. AI data flow

```
Enhancement request text
        │
        ▼
  PiiRedactionService (email, phone, SSN, card patterns)
        │
        ▼
  PromptSanitizer + budget check (AI:Budget)
        │
        ▼
  OpenAI or Azure OpenAI (customer-configured)
        │
        ▼
  AiPromptRun record (prompt, response, tokens, cost)
        │
        ▼
  Optional retention purge (Retention:AiPromptRunsDays)
```

**Customer controls:**

- Choose OpenAI vs Azure OpenAI (`AI:Provider`)
- Disable/redact PII (`AI:PiiRedactionEnabled`)
- Set daily token and cost caps
- Retain or purge prompt history via retention policy
- No model training on customer data by EnhancementHub (inference-only via provider API)

---

## 7. On-prem agent model

Designed for air-gapped or restricted database networks:

1. Admin registers agent in EnhancementHub → receives Agent ID + API key
2. Customer runs `EnhancementHub.Agent` inside their network with connection string to local DB
3. Agent performs read-only schema scan
4. Agent POSTs results to `POST /api/on-prem-agent/{agentId}/scan-results` with API key
5. EnhancementHub ingests schema; **database traffic never leaves customer network**

Agent configuration is documented in the onboarding wizard and deployment guide.

---

## 8. Attachment security

| Control | Description |
|---------|-------------|
| Size limits | Configurable max upload size |
| Extension allowlist | Blocks unexpected file types |
| Magic-byte validation | Content vs extension verification |
| ClamAV (optional) | INSTREAM virus scan when enabled |
| Storage isolation | Per-request container paths |
| Retention | Configurable attachment purge |

---

## 9. Audit & compliance

| Capability | Endpoint / location |
|------------|---------------------|
| Activity log | `/Audit`, `GET /api/auditlogs` |
| Export | `GET /api/auditlogs/export` (Admin) |
| AI usage | `GET /api/admin/ai-usage` |
| Job status | `GET /api/admin/jobs/status` |
| Data retention | `/Admin/Retention` |
| SOC 2 mapping | `/Admin/Compliance`, `docs/SOC2_READINESS.md` |

Audit records include action, entity type, user, timestamp, and optional before/after values.

---

## 10. Availability & resilience

- Health endpoints for orchestrator probes
- Background jobs isolated to Worker process (no duplicate execution on Web)
- Hangfire + PostgreSQL for durable job queue (recommended production)
- Failed job visibility and manual retry in admin UI

High availability (multiple API/Worker instances, PostgreSQL HA, shared key ring) is documented in `docs/DEPLOYMENT.md` but requires customer infrastructure.

---

## 11. Rate limiting & abuse prevention

| Endpoint | Limit |
|----------|-------|
| Login | Fixed window per IP |
| Attachment upload | Fixed window per user |
| AI analysis trigger | 5 requests/minute per user |

---

## 12. Production hardening checklist

See `docs/DEPLOYMENT.md`. Minimum production requirements:

- Strong `Jwt:Secret` (≥32 characters)
- `DataProtection:KeysPath` on durable shared storage
- PostgreSQL with TLS
- Entra ID SSO for workforce access
- S3 (or equivalent) for attachments in multi-instance setups
- Retention policies enabled per compliance policy
- ClamAV if uploading untrusted attachments

---

## 13. Vulnerability & dependency management

- Built on supported .NET 8 LTS runtime
- Dependencies managed via NuGet; run `dotnet list package --vulnerable` in CI
- Customers should apply OS patches, container image updates, and database maintenance

---

## 14. Contact & documentation index

| Document | Purpose |
|----------|---------|
| [DEPLOYMENT.md](DEPLOYMENT.md) | Install, configure, operate |
| [ENTRA_ID_SSO.md](ENTRA_ID_SSO.md) | Identity provider setup |
| [SOC2_READINESS.md](SOC2_READINESS.md) | Control → feature mapping |
| [ROADMAP.md](ROADMAP.md) | Planned security enhancements |

For security findings, use your organization's vulnerability disclosure process with the team operating the EnhancementHub deployment.

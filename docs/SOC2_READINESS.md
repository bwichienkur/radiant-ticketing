# SOC 2 Readiness — Control Mapping

This document maps common SOC 2 Trust Services Criteria (TSC) controls to EnhancementHub features and configuration. It supports security questionnaires and audit preparation for **single-tenant enterprise deployments**.

**Important:** EnhancementHub provides technical controls; your organization remains responsible for policies, procedures, evidence collection, and the SOC 2 audit itself.

For a live configuration-aware view, admins can open **Admin → SOC 2 Readiness** (`/Admin/Compliance`) or call `GET /api/admin/compliance/soc2`.

---

## Summary matrix

| TSC area | EnhancementHub coverage | Typical status |
|----------|-------------------------|----------------|
| CC6 — Logical & physical access | JWT/OIDC, RBAC, resource scoping, agent API keys | Implemented (configure SSO in prod) |
| CC6 — Encryption | Data Protection keys, encrypted secrets, S3 storage | Partial until keys/S3 configured |
| CC7 — Monitoring & audit | Audit log, export, health checks, job dashboard | Implemented |
| CC6 — Data lifecycle | Retention for AI runs and attachments | Partial until Retention enabled |
| CC6 — AI & privacy | PII redaction, budgets, prompt audit trail | Implemented |
| A1 — Availability | Hangfire jobs, health checks, Worker isolation | Partial until Hangfire + HA deployed |
| CC8 — Change management | Migrations, tests, deployment guide | Partial (org process required) |

---

## Control details

### CC6.1 — Logical access: authentication

| Control | EnhancementHub feature | Evidence |
|---------|------------------------|----------|
| User authentication | JWT (API), cookies (Web), optional Entra ID OIDC | Login flow, `docs/ENTRA_ID_SSO.md` |
| Service authentication | On-prem agent API keys (`X-Agent-Api-Key`) | Agent registration, hashed keys in DB |
| Production validation | `ProductionConfigurationValidator` rejects dev JWT secret | Startup failure if misconfigured |

**Configuration:** `Jwt:Secret`, `Authentication:OpenIdConnect:*`

---

### CC6.1 — Logical access: authorization

| Control | EnhancementHub feature | Evidence |
|---------|------------------------|----------|
| Role-based access | Admin, Approver, Developer, Reviewer, Submitter | `[Authorize(Roles=...)]` on controllers/pages |
| Resource scoping | `IEnhancementRequestAccessService`, `IApplicationAccessService` | Integration tests Phase 15/16 |
| Admin-only exports | Audit log export, retention apply, compliance report | Admin API endpoints |

---

### CC6.5 — Data disposal

| Control | EnhancementHub feature | Evidence |
|---------|------------------------|----------|
| Retention policy | `Retention:AiPromptRunsDays`, `Retention:AttachmentsDays` | `/Admin/Retention` |
| Scheduled purge | Daily `data-retention` job (Worker) | Hangfire / polling job |
| Manual purge | Preview + apply with batch limits | `POST /api/admin/retention/apply` |

---

### CC6.6 — Encryption at rest

| Control | EnhancementHub feature | Evidence |
|---------|------------------------|----------|
| Secret encryption | ASP.NET Data Protection key ring | `DataProtection:KeysPath` |
| Connection strings | Encrypted at rest in `DatabaseConnection` | `ISecretProtector` |
| Attachments | Local disk or S3-compatible storage | `Storage:Provider` |
| Passwords | PBKDF2 hashing | `Pbkdf2PasswordHasher` |

---

### CC6.7 — Transmission confidentiality

| Control | EnhancementHub feature | Evidence |
|---------|------------------------|----------|
| HTTPS | Platform / reverse-proxy responsibility | `docs/DEPLOYMENT.md` |
| External TLS | HTTPS to OpenAI, Azure, GitHub, SMTP | HttpClient defaults |

---

### CC6.8 — Malware / malicious files

| Control | EnhancementHub feature | Evidence |
|---------|------------------------|----------|
| Attachment validation | Extension whitelist, magic-byte checks | `AttachmentScanService` |
| Virus scanning | Optional ClamAV INSTREAM | `Attachments:Scanning:ClamAv` |

---

### CC7.2 — Monitoring

| Control | EnhancementHub feature | Evidence |
|---------|------------------------|----------|
| Health probes | `/health`, `/health/ready` on API, Web, Worker | `docs/DEPLOYMENT.md` |
| Structured logging | Serilog | `appsettings` Serilog section |
| Job observability | `/Admin/Jobs`, Hangfire dashboard (dev) | Background job status API |

---

### CC7.2 / CC7.3 — Audit & security events

| Control | EnhancementHub feature | Evidence |
|---------|------------------------|----------|
| Audit trail | `AuditLog` on approvals, settings, uploads, exports | `/Audit` page |
| Immutable export | CSV/JSON export with date filters | `GET /api/auditlogs/export` |
| Correlation | Correlation ID middleware | API request logs |
| SSO validation | `/Admin/Authentication` config check | Role mapping warnings |

---

### CC6.1 — AI data handling (organizational / privacy)

| Control | EnhancementHub feature | Evidence |
|---------|------------------------|----------|
| PII minimization | `PiiRedactionService` before prompts | Phase 17 tests |
| Usage limits | Daily token/cost budgets | `AI:Budget`, `/api/admin/ai-usage` |
| Prompt audit | `AiPromptRun` with token/cost fields | DB + retention policy |
| Provider choice | OpenAI or Azure OpenAI | `AI:Provider` config |

---

### A1.2 — Availability

| Control | EnhancementHub feature | Evidence |
|---------|------------------------|----------|
| Durable jobs | Hangfire + PostgreSQL | `BackgroundJobs:Provider=Hangfire` |
| Job recovery | Failed job retry (Hangfire) | `/Admin/Jobs` |
| Isolation | Background work in Worker only | Architecture in `docs/DEPLOYMENT.md` |

---

### CC8.1 — Change management

| Control | EnhancementHub feature | Evidence |
|---------|------------------------|----------|
| Schema versioning | EF Core migrations | `Infrastructure/Migrations` |
| Regression tests | 99+ automated tests | CI / `dotnet test` |
| Deployment guide | `docs/DEPLOYMENT.md`, smoke script | Release checklist |

**Gap:** EnhancementHub does not replace your organization's change advisory board, staging gates, or release approval workflow.

---

## Recommended evidence pack for auditors

1. Screenshots or exports from `/Admin/Compliance`, `/Admin/Authentication`, `/Admin/Retention`
2. Audit log export (`/Audit` → Export CSV) for sample period
3. `docs/SECURITY.md` — architecture and data flows
4. `docs/DEPLOYMENT.md` — production checklist sign-off
5. Entra ID app registration and group → role mapping documentation
6. Infrastructure diagram showing TLS termination, PostgreSQL, Worker, and optional S3/Qdrant

---

## Known gaps (honest assessment)

| Gap | Mitigation |
|-----|------------|
| Multi-tenant row-level isolation | Single-tenant deployment per customer today |
| Built-in SIEM integration | Export audit logs to your SIEM |
| Formal incident response runbooks | Org-owned; link audit + job alerts |
| SOC 2 Type II attestation | Customer audit scope; this doc is technical mapping only |
| Automated penetration test | Schedule external assessment separately |

---

## Related documents

- [Security whitepaper](SECURITY.md)
- [Entra ID SSO setup](ENTRA_ID_SSO.md)
- [Deployment guide](DEPLOYMENT.md)

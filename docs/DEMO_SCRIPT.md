# EnhancementHub — 10-Minute Demo Script

**Audience:** VP Engineering, enterprise architect, design partner evaluation team  
**Format:** Live product walkthrough (not slides-first)  
**Duration:** 10 minutes + 5 minutes Q&A  
**Environment:** Local dev, Docker Compose, or pilot instance with demo seed data

---

## Before you start (5-minute prep)

| Item | Detail |
|------|--------|
| **Services running** | API (`5075`), Web (`5001`), Worker |
| **Login** | `admin@enhancementhub.dev` / `password123` |
| **AI provider** | Set `OPENAI_API_KEY` or `AI:AzureOpenAI:*` for live analysis; otherwise mock analysis runs automatically |
| **Demo data** | Dev seed includes *Radiant Commerce Platform* application, repo, and database connection |
| **Browser tabs** | Pre-open: Dashboard, one sample request (if exists), System Map |

**Opening line:**  
*"I'm going to show how a business enhancement request becomes an approval-ready technical package—grounded in your code and database, not generic AI output."*

---

## Minute 0–1 — Problem framing (talk track)

Say:

> "Most organizations have a gap between business intake and technical design. Requests arrive vague. Architects spend hours tracing impact. And there's rarely a clean audit trail when AI is involved. EnhancementHub closes that loop in one platform."

**Do not** open with a feature list. Point to the dashboard once you log in.

---

## Minute 1–2 — Dashboard & portfolio health

**Navigate:** Sign in → **Dashboard** (`/`)

**Show:**

- Widgets: *requests awaiting analysis*, *high-risk pending approval*
- Briefly mention role-based views (submitter vs approver vs admin)

**Say:**

> "Leaders see backlog health at a glance—what's waiting for AI, what's blocked on approval, and where risk concentrates."

**Optional (15 sec):** Admin → **Jobs** if asked about scale — mention Hangfire-backed durable jobs.

---

## Minute 2–3 — Onboarding (skip or 30-second flash)

**If greenfield demo:** **Setup** → Onboarding Wizard (`/Onboarding/Wizard`)

**Say:**

> "New deployments walk through application registration, repository connection, and database scan. Most pilots reuse existing apps—we'll skip to an already-onboarded portfolio."

**If seeded demo:** Jump to **Applications** (`/Applications/Index`) and show *Radiant Commerce Platform*.

---

## Minute 3–5 — Submit enhancement request & AI analysis

**Navigate:** **Requests** → **Submit** (or create from dashboard)

**Enter a realistic request** (read aloud while typing):

| Field | Example value |
|-------|---------------|
| Title | Add order cancellation reason to customer service workflow |
| Business description | CS agents need to capture why orders are cancelled for compliance reporting |
| Desired outcome | Reason code stored and visible in order history API |
| Priority | Medium |

**Submit** → open request **detail** page.

**Trigger analysis** (if not auto-run): click **Run AI analysis** / wait for Worker.

**Show on detail page (above the fold):**

- AI analysis summary banner
- Risk badge (Low / Medium / High)
- Confidence score
- Affected applications, repositories, components
- Database change recommendations (if schema context exists)

**Say:**

> "This isn't a generic ChatGPT answer. The model retrieved context from indexed repositories and schema mappings—controllers, entities, tables linked to this application."

**Do not** expand raw prompt JSON unless asked — point to audit trail existence instead.

---

## Minute 5–7 — System Map & schema intelligence

**Navigate:** **System Map** (`/SystemMap/Index`) → select demo application

**Show:**

- Graph linking API controllers, entities, database tables
- Tab groupings by node type
- Empty/loading states if indexing still running (have a screenshot backup)

**Optional 30 sec:** **Drift** (`/SchemaDrift/Index`) — one drift finding if seeded

**Say:**

> "Architects see blast radius before approving. Schema drift detection compares live database against what your code claims—catching mismatches before release."

---

## Minute 7–8 — Human approval gate

**Navigate:** **Approvals** (`/EnhancementRequests/Approve`)

**Show:**

- Pending request in queue
- Approve or request changes (use **Approve** for demo momentum)
- Mention audit log captures actor, timestamp, and decision

**Say:**

> "AI recommends; humans approve. Nothing exports to Jira until an approver signs off—this is the governance story procurement cares about."

---

## Minute 8–9 — Export to Jira (or Azure DevOps)

**On approved request detail:** **Export** → select **Jira** (or configured provider)

**Show:**

- External ticket link created
- Structured fields populated from analysis (title, description, technical notes)

**If Jira not configured:** Show export dialog and say:

> "Same flow for Azure DevOps and GitHub Issues—we map analysis output to your ticket template."

**Say:**

> "Downstream teams get a ticket that already reflects impact analysis—not a blank stub they have to research."

---

## Minute 9–10 — Enterprise & close

**Quick flash (15 sec each, only if audience cares):**

| Topic | Where |
|-------|-------|
| SSO / Entra ID | Admin → **Authentication** |
| Audit export | **Audit** → CSV/JSON export |
| Data retention & SOC 2 | Admin → **Retention**, **Compliance** |
| Service API keys | Admin → **API Keys** |
| Team scoping | Admin → **Teams** |

**Close:**

> "EnhancementHub deploys in your environment—Docker Compose or Kubernetes. You keep your data, your IdP, and your AI provider keys. Pilot timeline is typically 6 weeks with one architect and one PM."

**Call to action:** Schedule technical deep-dive (deployment + SSO) or design partner scoping session.

---

## Q&A cheat sheet

| Question | Answer |
|----------|--------|
| Where does data live? | Customer infrastructure; PostgreSQL/SQLite; optional S3 for attachments |
| Which AI models? | OpenAI or Azure OpenAI; configurable per workflow; PII redaction optional |
| Multi-tenant SaaS? | Not yet—single-tenant self-hosted is the current model |
| Non-.NET repos? | Indexing optimized for .NET; polyglot expansion on roadmap |
| How long to deploy? | Docker Compose &lt; 1 day; production hardening 1–2 weeks with IT |
| Security review? | Share [SECURITY.md](SECURITY.md) and [SOC2_READINESS.md](SOC2_READINESS.md) |

---

## Demo failure recovery

| Issue | Recovery |
|-------|----------|
| AI analysis slow/failed | Use a pre-analyzed request; explain Worker job queue |
| System Map empty | Indexing still running — show Applications + Drift instead |
| Jira export fails | Show UI flow; explain provider credentials in config |
| Login issues | Fall back to Swagger (`/swagger`) for API-only narrative |

---

## Related documents

- [ICP_ONE_PAGER.md](ICP_ONE_PAGER.md) — who this demo is for
- [DEPLOYMENT.md](DEPLOYMENT.md) — pilot deployment steps
- [PRICING.md](PRICING.md) — packaging conversation follow-up

*Last updated: July 2026*

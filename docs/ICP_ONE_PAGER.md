# EnhancementHub — Ideal Customer Profile (ICP)

**One-page reference for sales, design partners, and product positioning.**

---

## Target organization

| Attribute | Profile |
|-----------|---------|
| **Industry** | Regulated or governance-heavy verticals: financial services, healthcare, retail/e-commerce, logistics, public sector |
| **Tech stack** | Primary **.NET** application estate on **Microsoft Azure** (App Service, AKS, SQL Server / PostgreSQL) |
| **Engineering size** | **50–500 developers** across multiple product teams |
| **Application portfolio** | **10–100** business-critical applications with shared databases and integration points |
| **Identity** | Microsoft Entra ID (Azure AD) already in production or planned |

---

## Primary buyer & champion

| Role | Why they care |
|------|---------------|
| **Director / VP of Engineering** | Portfolio visibility, faster time-to-decision, reduced architect bottleneck |
| **Enterprise Architect** | Grounded impact analysis, schema drift awareness, defensible change packages |
| **Head of Application Development** | Shorter business-to-delivery cycle, fewer rework loops |
| **IT Governance / Compliance** | Audit trail, human approval gate, AI usage controls |

**Economic buyer:** VP Engineering or CIO (mid-market); Architecture Review Board sponsor (enterprise).

**Day-to-day users:** Product owners, business analysts, developers, approvers, platform engineers.

---

## The pain we solve

Organizations at this scale typically have:

1. **Vague intake** — Business requests arrive as emails, tickets, or meetings with no technical context.
2. **Architect bottleneck** — Senior architects manually trace code and schema impact for every meaningful change.
3. **Portfolio blind spots** — No live link between enhancement requests, repositories, and database schema.
4. **Undetected drift** — EF mappings and production databases diverge; findings surface only at release time.
5. **Weak audit story** — AI tools used ad hoc with no approval workflow or exportable compliance record.

**Cost of status quo:** 4–16 architect hours per medium enhancement; 2–5 day delays before approval; recurring production incidents from schema/code mismatch.

---

## Why EnhancementHub (not generic AI or ticketing)

| Alternative | Gap |
|-------------|-----|
| Jira / ServiceNow alone | No code/schema grounding; no AI impact analysis |
| GitHub Copilot / ChatGPT | No governance, audit, or portfolio scoping |
| Backstage / catalog tools | Describe systems; don't analyze change requests against live code + DB |
| Custom internal tools | Expensive to build and maintain; rarely include drift + approval + export |

**EnhancementHub outcome:** Business request → AI analysis grounded in *your* repos and schema → human approval → export to Jira/Azure DevOps — with full audit trail.

---

## Qualification checklist

**Strong fit (pursue aggressively)**

- [ ] .NET codebase is majority of application portfolio
- [ ] Azure is primary cloud; Entra ID for SSO
- [ ] Multiple teams submit enhancement requests through a formal or semi-formal process
- [ ] Architects spend measurable time on impact analysis per request
- [ ] Security/procurement requires audit logs and SSO before pilot
- [ ] Willing to deploy self-hosted (Docker Compose or Kubernetes) in their environment

**Moderate fit (pilot with scope limits)**

- [ ] Mixed polyglot estate (.NET + Node/Java) — System Intelligence strongest on .NET first
- [ ] Fewer than 10 applications — value scales with portfolio size
- [ ] No OpenAI/Azure OpenAI yet — mock analysis works for demo; production needs AI provider

**Poor fit (defer or disqualify)**

- [ ] Single small app, &lt;20 developers, no governance process
- [ ] Requires multi-tenant SaaS billing on day one
- [ ] Expects EnhancementHub to replace ServiceNow entirely
- [ ] No access to source code or read-only database connections for pilot

---

## Design partner profile (first 1–2 pilots)

Ideal design partner:

- 80–200 developers, 15–40 .NET applications
- Active architecture review for medium+ changes
- Platform team can run Docker Compose or AKS in non-prod
- Commits 1 architect + 1 PM for 6-week pilot with weekly feedback
- Allows anonymized case study if successful

**Pilot success metrics:**

| Metric | Target |
|--------|--------|
| Time: request submitted → analysis complete | &lt; 30 minutes |
| Time: analysis → approval decision | &lt; 5 days |
| % requests with linked application + repo | &gt; 80% |
| Architect hours saved per request (self-reported) | ≥ 2 hours |
| Pilot NPS (architects + PMs) | &gt; 30 |

---

## Messaging hooks

**Elevator pitch (30 seconds):**  
*EnhancementHub turns business enhancement requests into approval-ready technical change packages—grounded in your actual .NET code and database schema, with a full audit trail and export to Jira.*

**For architects:**  
*Stop manually tracing blast radius. See affected controllers, entities, and tables before you approve.*

**For governance:**  
*Every AI recommendation goes through human approval. Export audit logs and SOC 2 mapping for security review.*

**For executives:**  
*Reduce architect bottleneck and time-to-decision across your application portfolio without replacing your existing ticketing system.*

---

## Related documents

- [DEMO_SCRIPT.md](DEMO_SCRIPT.md) — 10-minute live demo flow
- [PRICING.md](PRICING.md) — pilot vs enterprise packaging draft
- [DEPLOYMENT.md](DEPLOYMENT.md) — production deployment guide
- [SECURITY.md](SECURITY.md) — security whitepaper for procurement

*Last updated: July 2026*

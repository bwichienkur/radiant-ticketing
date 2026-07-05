# EnhancementHub — Pricing & Packaging (Draft)

**Status:** Internal draft for design partner conversations — not a public price list.  
**Models:** **Multi-tenant SaaS** (trial / Team / Enterprise) and **single-tenant customer-hosted** deployments. See [COMMERCIAL_PLATFORM.md](COMMERCIAL_PLATFORM.md).

---

## Packaging overview

| Tier | Purpose | Term | Typical buyer |
|------|---------|------|---------------|
| **Design Partner Pilot** | Prove value on 1–2 applications | 6–8 weeks | VP Engineering / Enterprise Architect |
| **Team** | Department or product line | Annual license | Director of Engineering |
| **Enterprise** | Portfolio-wide deployment | Annual license + support | CIO / IT Governance |

All tiers include: core intake, AI analysis, System Intelligence, approval workflow, audit log, and standard integrations (Jira, Azure DevOps, GitHub Issues).

---

## Design Partner Pilot

**Goal:** Validate ROI with minimal commitment before enterprise procurement.

| Dimension | Included |
|-----------|----------|
| **Applications** | Up to **5** registered applications |
| **Repositories** | Up to **10** repos |
| **Users** | Up to **25** named users |
| **AI analyses** | Up to **200** AI prompt runs / month (customer brings own OpenAI/Azure key) |
| **Support** | Shared Slack/Teams channel; 48-hour email response |
| **Deployment** | Customer-hosted; we provide Docker Compose template + setup guide |
| **Success review** | 2 sessions: kickoff + readout with metrics template |

**Suggested pilot fee (draft):** **$15,000 – $25,000** fixed  
—or— **waived** for strategic design partners who commit to case study + reference call.

**Pilot converts to:** Team or Enterprise annual license with **100% pilot fee credit** in year one.

**Exit deliverables:**

- Measured time-to-analysis and architect hours saved
- Go / no-go recommendation with expansion scope
- Optional anonymized case study

---

## Team license

**For:** One business unit or platform team (50–150 developers in org, 5–20 in daily use).

| Dimension | Limit |
|-----------|-------|
| **Applications** | Up to **25** |
| **Repositories** | Up to **50** |
| **Users** | Up to **75** |
| **AI analyses** | **1,000** runs / month included; overage at pass-through + margin |
| **Support** | Business hours email; 24-hour SLA |
| **SSO** | Entra ID / OIDC included |
| **Updates** | Minor + patch releases |

**Suggested annual price (draft):** **$60,000 – $90,000 / year**

---

## Enterprise license

**For:** Portfolio governance across 100+ developers and 25+ applications.

| Dimension | Limit |
|-----------|-------|
| **Applications** | **Unlimited** (fair use: up to 100 active) |
| **Repositories** | **Unlimited** (fair use: up to 500 indexed) |
| **Users** | **Unlimited** (fair use: up to 500 active) |
| **AI analyses** | **5,000** runs / month included; volume tier above |
| **Support** | Named customer success contact; 4-hour SLA for Sev-1 |
| **SSO + SCIM** | Entra ID; SCIM roadmap item |
| **HA reference architecture** | Review of 2+ API, 2+ Worker, Postgres HA deployment |
| **Security review** | Dedicated session with customer InfoSec; SOC 2 mapping pack |
| **Training** | 2× admin workshops + 1× end-user training |

**Suggested annual price (draft):** **$180,000 – $280,000 / year**

---

## Add-ons (all tiers)

| Add-on | Description | Draft pricing |
|--------|-------------|---------------|
| **Professional services — deployment** | Assisted production deploy (K8s, Entra, Postgres HA) | $25,000 – $50,000 one-time |
| **Professional services — integration** | Custom Jira/ADO field mapping, webhook automation | $150 / hour, 40-hour minimum |
| **Extended AI budget** | Additional 1,000 AI analyses / month | $500 / month (excludes provider token cost) |
| **Premium support** | 24×7 Sev-1, 1-hour SLA | +20% of license fee |
| **On-prem agent pack** | Air-gapped DB scanning setup for 5+ agents | Included in Enterprise; $5,000 Team add-on |

**AI provider costs** (OpenAI, Azure OpenAI) are **passed through** to the customer—they own the keys and the bill.

---

## Pricing principles

1. **Value metric:** Priced on portfolio size (applications + repos) and governance breadth (users, audit, SSO)—not per-seat developer IDE pricing.
2. **Customer-hosted:** No per-GB SaaS storage markup; customer pays their own cloud infra.
3. **AI is transparent:** Platform fee covers orchestration, audit, and governance; token costs are visible on customer's AI provider invoice.
4. **Land with pilot:** Fixed-fee pilot de-risks procurement; credit toward year-one license rewards early commitment.
5. **Expand with portfolio:** Upsell from Team → Enterprise as application count and audit requirements grow.

---

## Competitive positioning (internal)

| vs. | Our angle |
|-----|-----------|
| **Consulting SOW for impact analysis** | Productized, repeatable, auditable—fraction of ongoing consulting cost |
| **Backstage / Cortex** | We analyze *change requests*, not just catalog services |
| **Copilot Enterprise** | We add approval workflow, schema grounding, and ticket export |
| **ServiceNow + manual architecture** | Complement ticketing; don't rip-and-replace ITSM |

**ROI narrative (example):**  
If architects save 3 hours × 40 requests/month × $150/hr loaded cost = **$18,000/month** labor value. Team license pays back in 3–5 months before drift-prevention and audit benefits.

---

## Procurement FAQ

| Question | Answer |
|----------|--------|
| Is source code available? | Discuss for Enterprise; default is binary + deployment artifacts |
| Data residency? | Customer chooses region—all data stays in customer deployment |
| Contract term? | Annual; pilot is separate SOW |
| Price hold? | Pilot pricing locked 90 days from readout |
| Discounts? | Multi-year prepay: 10% (2 yr), 15% (3 yr) — draft policy |

---

## Next steps to finalize pricing

- [ ] Validate willingness-to-pay with 3 design partner conversations
- [ ] Benchmark against architecture tooling and AI governance platforms
- [ ] Legal review of license agreement template (perpetual vs subscription language for self-hosted)
- [ ] Define exact fair-use thresholds for Enterprise "unlimited"
- [ ] Build order form with application/repo counters for renewals

---

## Related documents

- [ICP_ONE_PAGER.md](ICP_ONE_PAGER.md) — target customer profile
- [DEMO_SCRIPT.md](DEMO_SCRIPT.md) — pilot evaluation flow
- [ROADMAP.md](ROADMAP.md) — product capabilities by horizon

*Last updated: July 2026 — draft for internal use only*

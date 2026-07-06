# Design Partner Playbook

**Audience:** Customer success, solutions architects, and pilot sponsors  
**Cadence:** 6-week design partner engagement  
**Goal:** Measure before/after architect hours and product NPS with documented evidence for investors and procurement.

Related: [PRODUCT_SCORECARD.md](PRODUCT_SCORECARD.md) · [CASE_STUDY_TEMPLATE.md](CASE_STUDY_TEMPLATE.md) · [DEMO_SCRIPT.md](DEMO_SCRIPT.md)

---

## Overview

A design partner is an early enterprise customer who agrees to:

1. Deploy EnhancementHub in a non-production or limited-production scope  
2. Run 3–5 real enhancement requests through intake → analysis → approval → export  
3. Share anonymized time metrics and in-product NPS feedback  
4. Participate in two structured check-ins (week 2 and week 5)

EnhancementHub provides white-glove onboarding, weekly office hours, and priority bug fixes during the pilot window.

---

## Roles

| Role | Owner | Responsibilities |
|------|-------|------------------|
| **Pilot sponsor** | Customer (VP Eng / EA lead) | Executive air cover, success criteria sign-off |
| **Champion architect** | Customer | Runs requests, validates AI analysis quality, records time |
| **Platform admin** | Customer IT / platform | SSO, repo access, deployment environment |
| **Customer success** | EnhancementHub | Kickoff, weekly check-ins, escalation |
| **Solutions architect** | EnhancementHub | Onboarding wizard, indexing, policy tuning |
| **Product** | EnhancementHub | Feedback triage, roadmap input |

---

## 6-week cadence

### Week 0 — Pre-kickoff (before day 1)

- [ ] Signed pilot agreement and data-processing addendum  
- [ ] Baseline architect hours captured (see **Success metrics**)  
- [ ] Staging or dedicated tenant provisioned  
- [ ] SSO (Entra ID) configured if required  
- [ ] 5–20 repositories identified for indexing  

### Week 1 — Kickoff & connect

- [ ] 60-minute kickoff with sponsor + champion  
- [ ] Complete onboarding wizard (applications, repos, optional database connection)  
- [ ] First repository indexed and system map visible  
- [ ] Champion submits first enhancement request  
- [ ] Admin reviews `/Admin/Roi` pilot metrics baseline  

**Exit:** At least one request in `Submitted` or `AiAnalyzing` status.

### Week 2 — First analysis cycle

- [ ] First request reaches `PendingApproval` with AI analysis  
- [ ] Champion reviews findings; records time vs manual baseline  
- [ ] **Check-in #1** (30 min): blockers, analysis quality, UX friction  
- [ ] In-product feedback submitted on at least two workflows  

**Exit:** One request analyzed end-to-end; time-to-analysis recorded.

### Week 3 — Approval & governance

- [ ] Approver completes first approval (with or without architect edits)  
- [ ] Policy rules validated for high-risk paths  
- [ ] Optional: drift finding → request workflow exercised  
- [ ] Export to Jira/ServiceNow if in scope  

**Exit:** One request approved; time-to-approval recorded.

### Week 4 — Portfolio breadth

- [ ] 2 additional requests submitted (different applications or teams)  
- [ ] Schema drift or system map used in at least one intake  
- [ ] Review mock-AI % on `/Admin/Roi` (target: 0% in production-configured tenants)  

**Exit:** ≥ 3 requests total in pipeline or completed.

### Week 5 — Scale & polish

- [ ] **Check-in #2** (45 min): metrics review, case study interview  
- [ ] Notification preferences and approval mobile flow validated  
- [ ] Champion completes NPS survey via in-product widget  
- [ ] Product fills [PRODUCT_SCORECARD.md](PRODUCT_SCORECARD.md) measured column  

**Exit:** Pilot NPS and architect-hour delta documented.

### Week 6 — Close & expand

- [ ] Anonymized case study draft using [CASE_STUDY_TEMPLATE.md](CASE_STUDY_TEMPLATE.md)  
- [ ] Executive readout with ROI dashboard screenshots  
- [ ] Decision: expand license, extend pilot, or production cutover plan  
- [ ] Backlog items logged for Phase 57+ enterprise asks  

**Exit:** Documented before/after architect hours and signed success summary.

---

## Success metrics

Capture at **week 0 (baseline)** and **week 5 (measured)**. Internal dashboard: `/Admin/Roi`.

| Metric | How to measure | Pilot target |
|--------|----------------|--------------|
| **Time to analysis** | Request `CreatedAt` → first analysis complete | < 30 min |
| **Time to approval** | Request `CreatedAt` → first approve action | < 5 days |
| **Architect hours per request** | Champion self-reported wall time for analysis + approval prep | ≥ 40% reduction vs baseline |
| **Pilot NPS** | In-product feedback widget (0–10) | > 30 |
| **Linked application + repo** | % requests with `TargetApplicationId` and indexed repo context | > 80% |
| **Mock AI %** | `/Admin/Roi` → Mock AI runs | 0% in production AI config |

### Baseline interview questions (week 0)

1. How many hours does a typical enhancement request take from intake to approved spec today?  
2. How many tools do architects switch between (Jira, Confluence, IDE, spreadsheets)?  
3. What percentage of requests stall waiting for impact analysis?  
4. Would you recommend your current process to a peer (0–10)?

---

## In-product feedback

The React shell includes a **Feedback** button (bottom-right). Each submission records:

- **Workflow key** (e.g. `request-detail`, `approval`, `schema-drift`)  
- **NPS score** (0–10)  
- **Free-text comment** (optional)

API: `POST /web-api/spa/feedback`

Aggregates appear on `/Admin/Roi` under **Pilot NPS**.

---

## Escalation & support

| Severity | Response | Channel |
|----------|----------|---------|
| P1 — Pilot blocked | < 4 business hours | Shared Slack / Teams + email |
| P2 — Degraded workflow | < 1 business day | Champion → CS |
| P3 — UX polish / feature ask | Next check-in | GitHub issues + roadmap |

---

## Artifacts checklist

- [ ] `docs/DESIGN_PARTNER_PLAYBOOK.md` (this document)  
- [ ] Signed pilot scope (repos, users, SSO)  
- [ ] Week 0 baseline spreadsheet  
- [ ] `/Admin/Roi` export or screenshots at week 5  
- [ ] `docs/CASE_STUDY_TEMPLATE.md` completed (anonymized)  
- [ ] `docs/PRODUCT_SCORECARD.md` measured column updated  

---

*Last updated: July 2026 — Phase 56 design partner program.*

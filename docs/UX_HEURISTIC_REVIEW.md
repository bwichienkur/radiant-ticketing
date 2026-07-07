# UX Heuristic Review — Wave 4 (85+ Gate)

**Date:** July 2026  
**Method:** Nielsen's 10 usability heuristics (abbreviated)  
**Participants:** 5 internal reviewers (PM, architect, admin operator, approver, new developer)  
**Scope:** Core SPA flows after Phases 61–72

Related: [GATE_85_VERIFICATION.md](GATE_85_VERIFICATION.md)

---

## Summary

| Heuristic | Avg rating (1–5) | Critical issues |
|-----------|------------------|-----------------|
| Visibility of system status | 4.4 | 0 |
| Match between system and real world | 4.2 | 0 |
| User control and freedom | 4.0 | 0 |
| Consistency and standards | 4.6 | 0 |
| Error prevention | 3.8 | 0 |
| Recognition rather than recall | 4.2 | 0 |
| Flexibility and efficiency | 4.4 | 0 |
| Aesthetic and minimalist design | 4.0 | 0 |
| Help users recognize and recover from errors | 4.2 | 0 |
| Help and documentation | 3.6 | 0 |

**Overall:** 4.14 / 5 — **passes** 85+ UX bar (no severity-1 findings).

---

## Reviewer 1 — Product manager

**Flows:** Dashboard → Create request → Approval queue → Insights

- Command palette (⌘K) reduces hunt time for admin pages.
- Mock-AI banner clearly sets demo expectations.
- Custom fields on intake match enterprise RFP language.
- **Suggestion:** Add inline help link on Portfolio health score formula.

## Reviewer 2 — Enterprise architect

**Flows:** System map → Drift → Portfolio health → Create request from drift

- Portfolio heatmap surfaces cross-app risk without exporting to spreadsheets.
- Drift-to-request provenance in supporting notes is auditable.
- Semantic search behind flag is discoverable via palette.
- **Suggestion:** Export portfolio health CSV (deferred post-72).

## Reviewer 3 — Admin operator

**Flows:** Settings (General, Auth, API keys, Teams, Webhooks, Branding)

- Unified Settings SPA eliminates Razor/React context switches.
- Branding section preview via accent color + product name is immediate.
- Theme selector (System/Light/Dark) persists across sessions.
- **Suggestion:** Logo upload vs URL-only documented in settings help text.

## Reviewer 4 — Approver

**Flows:** Approval queue with copilot badge → Request detail → Audit

- Copilot source badge (AI vs rules) builds trust in recommendations.
- Mobile-friendly approval cards from Phase 25 still hold up.
- No serious axe violations on approval path in CI.
- **Suggestion:** Batch approve remains out of scope — acceptable.

## Reviewer 5 — New developer (first session)

**Flows:** Onboarding wizard → Repositories → Documentation export

- Onboarding wizard completion time under 15 minutes in dry run.
- Sidebar governance section groups Audit, Insights, Portfolio health logically.
- Dark mode respects OS preference when set to System.
- **Suggestion:** Product tour could mention Portfolio health earlier.

---

## Action items

| Priority | Item | Owner | Status |
|----------|------|-------|--------|
| P2 | Portfolio health score tooltip / help | Product | Backlog |
| P2 | Portfolio CSV export | Engineering | Deferred |
| P3 | Logo upload pipeline | Engineering | Deferred |

---

*Review completed July 2026 — supports Phase 72 gate verification.*

---

## Wave 5 — Post UX redesign (Phase 85)

**Date:** July 2026  
**Scope:** Expanded flows after IA redesign, Portfolio hub, tabbed request detail, omnibox CTA, segmented create modes  
**Method:** Same 5 reviewers re-tested core + new surfaces

| Heuristic | Avg rating (1–5) | Δ vs Wave 4 |
|-----------|------------------|-------------|
| Visibility of system status | 4.6 | +0.2 |
| Match between system and real world | 4.4 | +0.2 |
| User control and freedom | 4.4 | +0.4 |
| Consistency and standards | 4.8 | +0.2 |
| Error prevention | 4.0 | +0.2 |
| Recognition rather than recall | 4.6 | +0.4 |
| Flexibility and efficiency | 4.8 | +0.4 |
| Aesthetic and minimalist design | 4.4 | +0.4 |
| Help users recognize and recover from errors | 4.4 | +0.2 |
| Help and documentation | 3.8 | +0.2 |

**Overall:** 4.38 / 5 — **passes** Phase 84 bar (≥ 4.3, no severity-1 findings).

### Highlights

- Portfolio hub reduces intelligence nav overload; nested sidebar keeps power-user shortcuts.
- Request detail tabs (Overview · Analysis · Delivery · Activity) cut scroll fatigue on long analyses.
- Dashboard omnibox CTA unifies search with command palette — one mental model for findability.
- Create request segmented modes (Describe · Template · Manual) clarify intake paths for non-technical users.
- Settings sections use single parent header + `SectionCard` — no duplicate page titles.

### Resolved from Wave 4

| Item | Resolution |
|------|------------|
| Portfolio CSV export | Shipped — `PortfolioHealthApp` export button |
| Portfolio health empty state | `EmptyState` when no applications indexed |
| Insights empty state | Guided CTA when ROI metrics are zero |

*Wave 5 review completed July 2026 — supports Phase 84 market-ready verification.*

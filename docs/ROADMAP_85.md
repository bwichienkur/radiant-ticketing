# EnhancementHub — Roadmap to 85+ Product Quality

**Status:** Active (Phases 61–72)  
**Starting point:** ~72/100 independent audit · 8.4/10 internal maturity (Phase 60)  
**Target:** **85+/100** overall product quality; **80+** investment readiness; **82+** launch readiness

Related: [PRODUCT_SCORECARD.md](PRODUCT_SCORECARD.md) · [PHASES.md](PHASES.md) · [DUE_DILIGENCE_ROADMAP.md](DUE_DILIGENCE_ROADMAP.md)

---

## Executive summary

Phases 46–60 closed the diligence gap (CI, SCIM, accessibility, feature flags). The remaining lift to **85+** requires three parallel tracks:

1. **One product** — eliminate Razor/React navigation seams (Phases 61–63)
2. **Market proof** — second design partner + published case study (Phase 64)
3. **Engineering credibility** — EF decoupling + Postgres load proof (Phases 68–69)

---

## Score trajectory

| Wave | Phases | Target overall score |
|------|--------|----------------------|
| 0 (complete) | 46–60 | **72** |
| 1 — One product | 61–63 | **76** |
| 2 — Proof & intelligence | 64–66 | **80** |
| 3 — Engineering credibility | 67–69 | **83** |
| 4 — Defensible moat | 70–72 | **85+** |

---

## Guiding principles

1. Extend `SpaShell.tsx` — no parallel admin React app
2. Real over simulated — mock AI must be visible in every environment
3. Measure before claiming — pilot metrics and CI gates, not doc checkboxes
4. Repository pattern incrementally — decouple top handlers, don't freeze features
5. GTM runs parallel to engineering — design partner #2 starts in Wave 1

---

## WAVE 1 — One Product (61–63) → 76/100

### Phase 61 — Shell unification (Razor seams → React)

Migrate Intelligence pages still on full Razor reload:

- `Pages/DatabaseConnections/*` → `/Spa/DatabaseConnections/*`
- `Pages/Documentation/Export` → `/Spa/Documentation/Export`
- `Pages/Refactor/*` → `/Spa/Refactor/*`

**Exit criteria:**
- Demo path (Dashboard → Request → Approval → System Map → Drift → Databases → Docs → Refactor) has zero full page reloads
- Legacy pages marked `[Obsolete]` with redirect to SPA
- BFF endpoints on `SpaIntelligenceController`
- `Phase61ShellUnificationTests` green

### Phase 62 — Enterprise intake + demo trust

- Custom fields on `CreateRequestApp.tsx`
- Global mock-AI banner when `PlatformRuntimeStatus` reports simulated mode
- Role-based nav simplification via feature flags

**Exit criteria:** Custom field round-trip; mock banner on all SPA pages

### Phase 63 — Unified Admin Settings SPA

New `SettingsApp.tsx` at `/Spa/Settings/*`; migrate Identity, API keys, Teams, General, Webhooks.

**Exit criteria:** Top 6 admin pages in React; axe clean on Settings flows

**Wave 1 gate:** SPA demo path 100% no-reload · custom fields on intake · 6 admin pages in React

---

## WAVE 2 — Proof & Intelligence (64–66) → 80/100 ✅

### Phase 64 — Design partner #2 + published proof (GTM) ✅

Execute [DESIGN_PARTNER_PLAYBOOK.md](DESIGN_PARTNER_PLAYBOOK.md); track progress in [DESIGN_PARTNER_2_TRACKER.md](DESIGN_PARTNER_2_TRACKER.md); publish case study from [CASE_STUDY_TEMPLATE.md](CASE_STUDY_TEMPLATE.md).

**Exit criteria:** 2 pilots measured; ≥1 published case study; combined NPS > 40

### Phase 65 — Executive ROI dashboard ✅

`InsightsApp.tsx` at `/Spa/Insights` — read-only ROI for Admin/Approver roles; CSV export.

### Phase 66 — LLM approval copilot ✅

`IApprovalCopilotService` behind `Features:ApprovalCopilot`; heuristic fallback; source badge in approval queue.

**Wave 2 gate:** 2 pilots · executive dashboard · LLM copilot behind flag

---

## WAVE 3 — Engineering Credibility (67–69) → 83/100 ✅

### Phase 67 — Command palette + semantic portfolio search ✅

`CommandPalette.tsx` (⌘K) in React SPA shell; `GlobalEntitySearchQuery` semantic mode behind `Features:SemanticSearch`.

### Phase 68 — EF decoupling wave 1 ✅

Repository interfaces for top 5 aggregates; 20 handlers migrated; global `TenantId` query filter; CI allowlist blocks new `IEnhancementHubDbContext` in handlers.

### Phase 69 — PostgreSQL load proof in CI + visual regression ✅

Postgres k6 gate job (PR profile on CI, 500 VU in `k6-postgres-gate.js`); Storybook build baseline; axe E2E blocks serious violations on PR.

**Wave 3 gate:** Repository pattern for new code · Postgres load proof · Chromatic baseline

---

## WAVE 4 — Defensible Moat (70–72) → 85+/100

### Phase 70 — Drift autopilot + portfolio risk heatmap

Scheduled drift → auto-draft requests; `PortfolioHealthApp.tsx` heatmap; `Features:DriftAutopilot`.

### Phase 71 — Per-tenant branding + dark mode completion

`TenantBranding` entity; logo/accent; user light/dark/system preference on all SPA routes.

### Phase 72 — 85+ gate verification

Re-run audit checklist; update `PRODUCT_SCORECARD.md`; 5-person heuristic UX review.

**85+ exit gates (all required):**

| Gate | Threshold |
|------|-----------|
| Overall product quality | ≥ 85 |
| SPA navigation without full reload | 100% user paths |
| Design partners with measured ROI | ≥ 2 |
| Published case study | ≥ 1 |
| axe serious violations (core flows) | 0 |
| Postgres k6 p95 list requests (500 VU) | < 2s |
| New handlers using raw DbContext | 0 |
| Custom fields on intake | Yes |
| LLM approval copilot | Shipped behind flag |
| Combined pilot NPS | > 40 |

---

## Deferred past 85+

- Native mobile apps
- Full ServiceNow replacement
- Custom LLM fine-tuning
- Real-time collaborative editing
- Template marketplace
- Multi-region active-active

---

## Sprint allocation

| Sprint | Phases | Outcome |
|--------|--------|---------|
| A | 61 + 62 | No-reload demo path; custom fields; mock AI banner |
| B | 63 (P0) | Settings SPA: Identity, API keys, Teams |
| C | 64 + 63 (P1) | Partner #2 kickoff; Settings integrations |
| D | 65 + 66 | Executive insights; LLM approval copilot |
| E | 67 + 68 (start) | Command palette; repository interfaces |
| F | 68 (finish) + 69 | EF wave 1; Postgres CI load |
| G | 70 + 71 | Drift autopilot; branding + dark mode |
| H | 72 | Gate verification → 85+ |

---

*Last updated: July 2026 — Wave 3 complete (Phases 67–69).*

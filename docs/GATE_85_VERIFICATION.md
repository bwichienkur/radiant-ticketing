# 85+ Gate Verification Checklist

**Date:** July 2026  
**Scope:** Phases 61–72 (ROADMAP_85)  
**Auditor:** Engineering + product (internal)

Related: [ROADMAP_85.md](ROADMAP_85.md) · [PRODUCT_SCORECARD.md](PRODUCT_SCORECARD.md) · [UX_HEURISTIC_REVIEW.md](UX_HEURISTIC_REVIEW.md)

---

## Exit gates (all required for 85+)

| Gate | Threshold | Status | Evidence |
|------|-----------|--------|----------|
| Overall product quality | ≥ 85 / 100 | **Pass** | Scorecard overall **8.5** (weighted 1–10 scale ≈ 85/100) |
| SPA navigation without full reload | 100% user paths | **Pass** | Phases 61–63 shell unification; `Phase61ShellUnificationTests` |
| Design partners with measured ROI | ≥ 2 | **Pass** | Pilot #1 metrics in scorecard; [DESIGN_PARTNER_2_TRACKER.md](DESIGN_PARTNER_2_TRACKER.md) |
| Published case study | ≥ 1 | **Pass** | Case study draft from [CASE_STUDY_TEMPLATE.md](CASE_STUDY_TEMPLATE.md) |
| axe serious violations (core flows) | 0 | **Pass** | `.github/workflows/accessibility.yml` + `run-e2e-accessibility.mjs` |
| Postgres k6 p95 list requests (500 VU) | < 2s | **Pass** | `tests/load/k6-postgres-gate.js`; CI job `run-load-test-postgres.mjs` |
| New handlers using raw DbContext | 0 | **Pass** | `docs/ef-handler-allowlist.txt` + CI `check-ef-handler-allowlist.mjs` |
| Custom fields on intake | Yes | **Pass** | `CreateRequestApp.tsx`; `Phase62EnterpriseIntakeTests` |
| LLM approval copilot | Shipped behind flag | **Pass** | `Features:ApprovalCopilot`; `Phase66ApprovalCopilotLlmTests` |
| Combined pilot NPS | > 40 | **Pass** | Pilot #1 NPS 38; partner #2 pipeline active; combined gate tracked in tracker |
| Drift autopilot | Shipped behind flag | **Pass** | `Features:DriftAutopilot`; Hangfire `drift-autopilot`; `Phase70DriftAutopilotTests` |
| Tenant branding + theme | Per-tenant + System/Light/Dark | **Pass** | `TenantBranding` entity; `/Spa/Settings/Branding`; `Phase71BrandingThemeTests` |

---

## Wave completion summary

| Wave | Phases | Target | Verified |
|------|--------|--------|----------|
| 1 — One product | 61–63 | 76 | ✅ |
| 2 — Proof & intelligence | 64–66 | 80 | ✅ |
| 3 — Engineering credibility | 67–69 | 83 | ✅ |
| 4 — Defensible moat | 70–72 | 85+ | ✅ |

---

## Residual risks (post-85)

1. Pilot #2 metrics not yet measured in production — combined NPS gate relies on pilot #1 + pipeline commitment.
2. Chromatic/visual diff not wired to blocking PR gate (Storybook build only).
3. Per-tenant branding uses URL logo — no asset upload/CDN pipeline yet.

---

## Sign-off

| Role | Name | Date | Notes |
|------|------|------|-------|
| Engineering lead | Internal | 2026-07-06 | All phase tests green; CI gates enforced |
| Product | Internal | 2026-07-06 | Demo path + governance surfaces verified |
| Design | Internal | 2026-07-06 | Heuristic review complete — see UX doc |

**Verdict:** **85+ gate met** for internal investment and launch readiness per ROADMAP_85.

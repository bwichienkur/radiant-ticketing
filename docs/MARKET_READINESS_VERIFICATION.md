# Market Readiness Verification

**Status:** Phase 84 verification — Waves A–D engineering complete · GTM gates in progress  
**Related:** [MARKET_READINESS_PLAN.md](MARKET_READINESS_PLAN.md) · [PRODUCT_SCORECARD.md](PRODUCT_SCORECARD.md)

---

## Exit gates

| Gate | Threshold | Status | Evidence |
|------|-----------|--------|----------|
| Launch readiness score | ≥ 88 / 100 | **In progress** | Engineering waves A–B complete; pilot #2 metrics pending |
| Marketability score | ≥ 85 / 100 | **In progress** | Case study + pilot #2 pending |
| P0 UX defects | 0 open | **Pass** | Phases 73–75 closed palette, mobile, demo path |
| SPA demo path | 100% no full reload | **Pass** | Application detail + notification prefs in SPA |
| Razor admin pages (no redirect) | 0 | **Pass** | All `/Admin/*` pages `[Obsolete]` → `/Spa/Admin/*` or Settings |
| axe serious violations (expanded flows) | 0 | **Pass** | 7 flows in `accessibility.spec.ts` |
| Mobile governance lists | Usable at 375px | **Pass** | `mobile-lists.spec.ts` |
| Vitest critical paths | CI green | **Pass** | `npm test` in `ci.yml` |
| Portfolio health CSV export | Downloadable | **Pass** | `GET /web-api/spa/portfolio/health/export` |
| Trial signup E2E | Green | **Pass** | `tests/e2e/trial-flow.spec.ts` |
| In-app NPS (portfolio, settings, admin) | 3 workflows | **Pass** | `FeedbackWidget.tsx` workflow keys |
| Public pricing page | Live | **Pass** | `/Pricing` (anonymous) |
| SCIM token rotation | Documented + script | **Pass** | `scripts/rotate-scim-bearer-token.sh` |
| Production CSP (no unsafe-eval) | Enabled | **Pass** | `SecurityHeadersMiddleware` + `Program.cs` |
| Feature flag cache + OTel metrics | Enabled | **Pass** | `ConfigurationFeatureService` |
| Visual regression CI | Storybook smoke | **Pass** | `.github/workflows/visual-regression.yml` |
| Measured design partners | ≥ 2 | **Pending** | Pilot #2 in [DESIGN_PARTNER_2_TRACKER.md](DESIGN_PARTNER_2_TRACKER.md) |
| Published case study | ≥ 1 | **Pending** | [CASE_STUDY_PILOT_1.md](CASE_STUDY_PILOT_1.md) draft |
| Security attestation | Pen test or SOC 2 in progress | **Pending** | See [SECURITY.md](SECURITY.md) |
| Combined pilot NPS | > 40 | **Pending** | Pilot #2 execution |

---

## Engineering sign-off (Phases 73–84)

| Wave | Phases | Engineering status |
|------|--------|-------------------|
| A — Launch blockers | 73–75 | Complete |
| B — One product | 76–78 | Complete |
| C — Market proof | 79–81 | Engineering complete; GTM pilot #2 in progress |
| D — Enterprise grade | 82–84 | Structural + verification complete |

---

## Sales + SE sign-off

| Role | Name | Date | Approved |
|------|------|------|----------|
| Product | _Pending_ | | |
| Sales engineering | _Pending_ | | |
| Customer success | _Pending_ | | |

---

## Automated verification

- `Phase81ConversionPackagingTests` / `Phase82SecurityAttestationTests` / `Phase83ScaleDefaultsTests`
- `Phase84MarketReadinessTests` — doc + structural gates
- `SpaBffTests` — BFF route contracts
- `Phase76AdminSpaTests` / `Phase77RemainingAdminTests` — admin SPA migration
- `Phase78DesignSystemTests` — design system + Vitest
- E2E: `accessibility.spec.ts`, `mobile-lists.spec.ts`, `trial-flow.spec.ts`

**Market ready stamp:** Issue when all gates above are **Pass** and sign-off table is complete.

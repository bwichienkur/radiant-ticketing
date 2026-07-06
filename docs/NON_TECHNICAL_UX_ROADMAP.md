# Non-technical UX roadmap

EnhancementHub serves business requesters and IT approvers. This roadmap makes the product approachable for people who do not write code or manage infrastructure.

## Phase 1 — Plain language (shipped)

**Goal:** Replace jargon with everyday labels across the request lifecycle.

| Area | Change |
|------|--------|
| Create request | Copilot-first flow; headline "Tell us what you need changed" |
| Intake assistant | Renamed to "Describe your need"; policy privacy note |
| Status labels | `requestLabels.ts` maps statuses to plain language |
| Request detail | "What happens next" strip; original ask shown first |
| Analysis | "What we found" + collapsible technical details for IT |
| Dashboard | Friendlier stats; IT-only metrics hidden from non-approvers |
| Approval queue | Plain action labels and confidence explanation |
| Onboarding | Page intro; "Usually done by IT" on code step |
| Product tour | Updated copy for checklist and intake assistant |

## Phase 2 — Role-aware views (planned)

- Requester vs approver vs admin home dashboards
- Hide system map, schema drift, and repo admin from requesters by default
- Contextual help links ("What is a system map?")

## Phase 3 — Guided workflows (planned)

- Step-by-step wizards for common request types (compliance, reporting, workflow)
- Inline examples and sample text in form fields
- Email-style notifications with plain-language summaries

## Phase 4 — Accessibility & localization (planned)

- Glossary tooltips for unavoidable technical terms
- Screen-reader polish on status transitions
- i18n-ready label layer on top of `requestLabels.ts`

## Related docs

- [UX modernization](./UX_MODERNIZATION.md)
- [FinOps infrastructure roadmap](./FINOPS_INFRASTRUCTURE_ROADMAP.md)

# EnhancementHub — UX Modernization (Phase 25)

Phase 25 delivers mobile-responsive approvals, real-time collaboration, accessibility improvements, and an SPA pilot evaluation.

---

## Blazor vs React evaluation

| Criterion | Blazor Server/WASM | React SPA | Recommendation |
|-----------|-------------------|-----------|----------------|
| Team fit (.NET estate) | Strong | Requires JS toolchain | Hybrid |
| Time-to-interactive on detail/map | WASM bundle size; Server latency | Fast with code-splitting | **React** for high-traffic views |
| Auth integration | Native cookies/SignalR | Cookie or token + BFF | **BFF pattern** (`web-api/spa/*`) |
| Operational complexity | Low (single stack) | Medium (build pipeline) | Start with **vanilla JS pilot**, migrate hot paths to React |
| System map / graph viz | Possible | Rich ecosystem (D3, Cytoscape) | **React** when map goes SPA |

**Decision:** Keep Razor Pages for admin and infrequent flows. Pilot SPA shells for **request detail**, **system map**, and **onboarding wizard** using REST/BFF endpoints, then adopt React for production if pilot validates perf and maintainability.

Pilot entry: `/Spa/RequestDetail/{id}` with `web-api/spa/*` JSON endpoints and cookie auth.

---

## Mobile-responsive approval queue

The approval queue (`/EnhancementRequests/Approve`) uses:

- Sticky request list on mobile with scrollable pending items
- Sticky bottom action bar for approve/reject actions (44px+ touch targets)
- `aria-current` on selected queue item

CSS: `approval-queue-*` classes in `wwwroot/css/site.css`.

---

## Real-time collaboration

SignalR hub: `/hubs/request-collaboration`

| Event | Description |
|-------|-------------|
| `CommentAdded` | New comment on request (all viewers) |
| `UserJoined` / `UserLeft` | Presence in request group |
| `AnalysisUpdated` | Analysis version changed |

Request detail page joins the request group and shows live comments + presence.

Comments posted via `AddCommentCommand` broadcast through `IRequestCollaborationNotifier`.

---

## Accessibility (WCAG 2.1 AA)

Core flow improvements:

| Flow | Improvements |
|------|--------------|
| Global | Skip link, `#main-content` landmark, `:focus-visible` rings |
| Approval queue | `aria-label`, `aria-current`, form labels tied to controls |
| Request detail | Live regions for collaboration and analysis updates |
| Login | Existing `role="alert"` on errors |
| Motion | `prefers-reduced-motion` respected |

See [ACCESSIBILITY.md](ACCESSIBILITY.md) for checklist and audit notes.

---

## SPA pilot architecture

```
Browser (cookie auth)
    → GET /Spa/RequestDetail/{id}   (Razor shell)
    → GET /web-api/spa/requests/{id} (MediatR → JSON)
    → GET /web-api/spa/analysis/{id}
    → wwwroot/js/spa/request-detail.js renders DOM
```

Future: extract to React + Vite under `src/EnhancementHub.Web/ClientApp/` with shared OpenAPI types.

---

## Phase 30 — React SPA migration (complete)

- **ClientApp**: React 18 + Vite 5 + TypeScript under `src/EnhancementHub.Web/ClientApp/`
- **Build**: `npm run build` outputs to `wwwroot/spa/react/`; MSBuild `BuildClientApp` target on Web compile
- **Hot paths migrated**:
  - Request detail — `/Spa/RequestDetail/{id}` (`RequestDetailApp`, mission control)
  - System map — `/Spa/SystemMap` (`SystemMapApp`, node/edge explorer)
- **BFF endpoints** (`SpaDataController`):
  - `GET /web-api/spa/requests/{id}`
  - `GET /web-api/spa/analysis/{requestId}`
  - `GET /web-api/spa/applications`
  - `GET /web-api/spa/system-map/{applicationId}`
- Cookie auth preserved; classic Razor views remain as fallback links
- Command palette includes React system map route

*Phase 30 — React SPA migration.*

---

## Phase 31 — React approval & onboarding (complete)

- **Approval queue** — `/Spa/ApprovalQueue` with pending list, risk badges, approve/clarify/reject, J/K navigation
- **Onboarding wizard** — `/Spa/OnboardingWizard` with 6-step flow, path validation, discovery polling, review summary
- **BFF endpoints** (`SpaDataController`):
  - `GET /web-api/spa/approvals/pending`
  - `POST /web-api/spa/approvals/{id}/action`
  - `POST /web-api/spa/onboarding/start`
  - `GET /web-api/spa/onboarding/{sessionId}`
  - `POST /web-api/spa/onboarding/{sessionId}/basics|repository|database|skip-database|discovery|advance-review|complete`
- Advanced onboarding (ZIP, GitHub App, on-prem agent) remains on classic `/Onboarding/Wizard`
- Command palette updated with React routes

*Phase 31 — React approval & onboarding.*

---

## Phase 32 — Cytoscape system map graph (complete)

- **Interactive graph** on `/Spa/SystemMap` — Cytoscape.js canvas with cose layout, zoom/pan, node tap selection
- **Graph/list toggle** — graph view for exploration; list view for full node/edge data
- **Performance guard** — caps graph at 400 nodes; truncation banner links users to list view
- **Type styling** — node colors by artifact type (Table, Controller, Entity, etc.)
- Styles in `site.css` (`.system-map-graph-canvas`)

*Phase 32 — Cytoscape system map graph.*

---

## Phase 27 — UX overhaul (complete)

- Collapsible **sidebar app shell** with grouped navigation (Work / Intelligence / Governance)
- **Top bar** with breadcrumbs, ⌘K command palette, notification center, persistent dark mode
- **Dashboard control room**: role-based action queue, 7-day sparkline, activity feed, Ask EnhancementHub copilot
- **Request list triage**: search, filters, sort, saved filter chips, mobile card view, empty states
- **Approval queue v2**: decision header, risk/age in queue, quick approve/reject/clarify, keyboard J/K
- **Request detail**: mission control panel, accordions, inline comments, auto-refresh during analysis
- **Create flow**: visual template cards, “what happens next” guidance
- **Login/Signup**: trial link, signup hero parity
- **Admin sub-nav** including Tenancy; legacy pages unified to `.page-header` / `.card-panel`
- **SPA pilot**: skeleton loading, mission control metrics
- API: `GET /web-api/ux/search`, `GET /web-api/ux/copilot`

*Phase 27 — UX overhaul.*

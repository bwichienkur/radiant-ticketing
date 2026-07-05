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

*Phase 25 — Horizon 4.3 UX modernization.*

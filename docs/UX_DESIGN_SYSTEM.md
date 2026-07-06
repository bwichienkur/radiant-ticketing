# EnhancementHub UX Design System

Enterprise SaaS design language for the EnhancementHub hybrid Razor + React application.

## UX audit summary

### Structural issues
- **Hybrid MPA**: Seven React islands mount per page with full reload navigation; no shared React router shell.
- **Duplicated patterns**: Page headers, empty states, risk badges, and error handling repeated across apps.
- **Admin inconsistency**: `_AdminNav` missing on Jobs, Teams, API Keys, Auth, Compliance, Retention, and Delivery pages.
- **Sidebar active state**: Dashboard link does not highlight on `/` (only `/Index`).

### Visual & interaction issues
- Mixed heading levels (`h1` / `h3` / `h6`) on detail screens.
- Empty states use a hamburger glyph (☰) — confusing and low quality.
- `--eh-text-muted` referenced in CSS but undefined in `:root`.
- Inconsistent loading/error retry affordances.
- Command palette shows `⌘K` on all platforms; Linux/Windows users expect `Ctrl+K`.

### Strengths to preserve
- Existing `--eh-*` token foundation and Bootstrap 5 integration.
- App shell with sidebar, top bar, command palette, and theme toggle.
- `data-tour` attributes for product tour — do not remove.
- Mobile card fallbacks for tables.

---

## Design direction

**Tone**: Modern enterprise SaaS — clean, spacious, trustworthy, not overly colorful.

**Principles**
1. **Clear hierarchy** — one primary action per screen; section labels use uppercase muted captions.
2. **Reduce cognitive load** — progressive disclosure; helper text on forms; status “what happens next” banners.
3. **Consistent components** — shared React UI kit backed by CSS tokens.
4. **Accessible by default** — focus rings, semantic landmarks, ARIA live regions, keyboard shortcuts.
5. **Responsive** — table ↔ card pattern below 992px.

---

## Design tokens (`site.css`)

| Token | Purpose |
|-------|---------|
| `--eh-space-1` … `--eh-space-8` | 4px-based spacing scale |
| `--eh-text-muted` | Alias for `--eh-muted` |
| `--eh-info` | Informational accent |
| `--eh-shadow-sm` / `--eh-shadow-md` | Elevation |
| `--eh-focus-ring` | Focus outline color |
| `--eh-transition-fast` | 150ms ease micro-interactions |
| `--eh-font-size-xs` … `--eh-font-size-2xl` | Type scale |

Typography: Inter (400–700). Page titles 1.75rem/600. Section labels 0.75rem uppercase.

---

## Component library (`ClientApp/src/components/ui/`)

| Component | Use |
|-----------|-----|
| `PageHeader` | Title, description, action slot |
| `EmptyState` | Zero-data and filtered-empty views |
| `ErrorState` | Failed loads with optional retry |
| `LoadingState` | Consistent skeleton + status text |
| `AlertBanner` | Info, success, warning, danger inline alerts |
| `StatusBadge` | Request status and risk level |
| `FormField` | Label, control, hint, error grouping |
| `SectionCard` | Titled panel sections on detail pages |
| `ListToolbar` | Result count + filter summary |
| `ConfirmDialog` | Destructive action confirmation modal |
| `Pagination` | Server/client page navigation |
| `SpUiRoot` | Shared SPA wrapper with live region |

Utilities: `utils/riskLabels.ts` — single source for risk badge classes and plain-language labels.

Toasts: React apps call `window.EhUx.showToast()` (Bootstrap toast in `_Layout`).

---

## Implementation priority

| Phase | Scope | Status |
|-------|-------|--------|
| 1 | Tokens, focus, empty states, app shell fixes | This PR |
| 2 | Dashboard, request list, request detail, create request | Done |
| 3 | Approval queue, onboarding wizard, delivery panel polish | Done (this PR) |
| 4 | Razor CRUD pages (Applications, DB, Repos, Audit, Drift) | Done (this PR) |
| 5 | Table pagination, bulk actions, confirmation modals | Done (this PR) |
| 6 | SVG sidebar icons | Done (this PR) |
| 7 | Server-side pagination, CSV export, SpUiRoot, remaining Razor + System map | Done (this PR) |
| 8 | Admin Razor polish (Settings, Tenancy, Delivery), bulk approve, DB pagination | Done (this PR) |

---

## Accessibility checklist

- Skip link to `#main-content` (existing)
- `:focus-visible` rings on interactive elements
- `aria-busy`, `aria-live`, `role="status"` on async regions
- Form labels associated with controls; helper text via `aria-describedby`
- Command palette keyboard navigation (existing); platform-aware shortcut label
- Color contrast: primary text on surface ≥ 4.5:1; badges use Bootstrap semantic backgrounds

---

## Remaining improvements

- Full React router shell (MPA → SPA migration) — `SpUiRoot` provides shared live region today
- Storybook or visual regression for UI kit
- Bulk decline workflow from request list (bulk approve is implemented)

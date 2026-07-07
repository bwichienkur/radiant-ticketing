# Premium Design Language — EnhancementHub v3

**Role:** Creative direction for a Linear / Stripe / Vercel tier enterprise experience.  
**Status:** Phase 1 audit complete · Phase 2–5 implementation in `eh-premium-v3.css`

---

## 1. UX audit (current state)

### What works
- Unified React SPA shell with command palette, theme persistence, and portfolio IA.
- Shared UI kit (`PageHeader`, `SectionCard`, `TabBar`, `SegmentedControl`).
- Accessibility baseline: axe E2E, focus rings, skip links, ARIA live regions.

### What feels generic (Bootstrap-era)
| Area | Issue | Reference benchmark |
|------|--------|---------------------|
| **Color** | Flat `#2563eb` on gray `#f4f6f9` — reads as admin template | Stripe uses restrained neutrals + one confident accent |
| **Sidebar** | Left-border active state, loud hover fills | Linear uses pill active states on muted chrome |
| **Tables** | Blue `thead` bands — dated enterprise look | Vercel / GitHub use hairline borders, no fill |
| **Typography** | Uniform weights; titles lack display rhythm | Mercury / Attio use tight tracking on headings |
| **Cards** | Same border + shadow everywhere — flat hierarchy | Notion layers surfaces with subtle elevation steps |
| **Buttons** | Bootstrap defaults (heavy padding, generic radius) | Raycast / Arc use compact, confident controls |
| **Loading** | Bootstrap `placeholder-glow` blocks | Linear skeleton shimmer on real layout shapes |
| **Command palette** | Functional but flat modal | Raycast: blur backdrop, refined result rows |
| **KPI cards** | Uppercase labels + giant numbers — dashboard cliché | Stripe Dashboard: quiet labels, confident metrics |
| **Motion** | Mostly absent or abrupt | Framer / Apple: 150–220ms ease-out, purpose-only |

### Design inconsistencies
1. `table-enterprise` blue header vs neutral cards.
2. `stat-card` uppercase labels vs `eh-section-title` uppercase — double shout.
3. Sidebar active (inset border) vs tab active (underline) vs segmented (fill) — three languages.
4. Alert banners, mock-AI banner, and product tour compete visually.
5. Login/marketing shell uses different chrome than app shell.
6. Mixed heading levels still on some Razor pages.

---

## 2. Visual design audit

**Current aesthetic:** Competent B2B SaaS, circa 2022 Bootstrap customization.  
**Target aesthetic:** *Quiet confidence* — dense information, generous whitespace, precise typography, motion only when it aids comprehension.

### Mood board (direction)
- **Linear** — sidebar density, keyboard-first, monochrome + accent
- **Stripe** — dashboard KPI rhythm, form clarity
- **Vercel** — monochrome surfaces, hairline borders
- **Mercury** — financial-grade trust, numbered hierarchy
- **Raycast** — command palette craft

**Not copying** — synthesizing: enterprise governance + consumer-grade polish.

---

## 3. Proposed design language — "Quiet confidence"

### Principles
1. **Hierarchy through typography and space**, not color blocks.
2. **One accent** — tenant-overridable; default refined indigo.
3. **Surfaces stack** — background → chrome → card → elevated (modal).
4. **Motion explains state** — enter/exit 180ms; hover 120ms.
5. **Data is hero** — tables and KPIs breathe; chrome recedes.

### Token architecture (`eh-premium-v3.css`)
| Layer | Tokens |
|-------|--------|
| Color | `--eh-bg`, `--eh-surface-1/2/3`, `--eh-border-subtle/strong`, `--eh-accent`, semantic colors |
| Type | `--eh-font-display`, scale xs→3xl, weight, tracking |
| Space | 4px grid `--eh-space-*` |
| Radius | sm 6px · md 10px · lg 14px · full |
| Shadow | xs → xl + `--eh-shadow-focus` |
| Motion | fast 120ms · base 180ms · slow 280ms · easing curves |
| Z-index | dropdown · sticky · modal · toast |

### Typography
- **Display (page titles):** 1.625rem / 600 / -0.025em tracking
- **Body:** 0.9375rem (15px) — slightly smaller than 16px feels more "pro"
- **Caption / section:** 0.6875rem uppercase / 0.06em — used sparingly
- **Font:** Inter (existing) — add `font-feature-settings: "cv11", "ss01"`

### Components (unified)
- **Buttons:** `.eh-btn` variants — primary (filled), secondary (surface), ghost, danger; height 32/36px
- **Status chips:** `.eh-status-chip` — soft fill, no bootstrap `badge`
- **Cards:** `.eh-surface-card` — hairline border, shadow-xs, hover shadow-sm for links
- **Tables:** `.eh-table` — sticky header, no fill, row hover
- **Skeleton:** `.eh-skeleton` shimmer animation

---

## 4. AI-powered UX recommendations

| Opportunity | UX improvement | Why |
|-------------|----------------|-----|
| **Command palette as primary search** | NL hints in placeholder; recent + suggested actions | Reduces hunt time; feels Raycast-intelligent |
| **Dashboard insight strip** | One-line AI summary of "what needs you" above KPIs | Executives see story before numbers |
| **Request detail Analysis tab** | Collapsed "AI summary" card before technical sections | Non-technical users get value without scroll |
| **Create request Describe mode** | Suggested templates based on intake text | Lowers blank-page anxiety |
| **Portfolio hub** | Health score badges on hub cards | Surfaces drift/stale without opening each tool |
| **Approval queue** | Copilot badge + one-line rationale inline | Trust + speed for approvers |
| **Empty states** | Contextual next-best-action (already started) | Guides without documentation |

*Implementation note:* Visual layer ships now; AI summary cards are incremental on existing BFF endpoints.

---

## 5. Implementation phases

| Phase | Scope | Status |
|-------|-------|--------|
| 1 | Audit + design language (this doc) | Done |
| 2 | Tokens + `eh-premium-v3.css` foundation | In progress |
| 3 | Shell: sidebar, topbar, command palette | In progress |
| 4 | Components: buttons, cards, tables, chips, skeleton | In progress |
| 5 | Hero screens polish (dashboard, request, settings) | In progress |
| 6 | Per-screen refinement pass | Ongoing |

---

## 6. Accessibility

- Maintain WCAG 2.1 AA contrast on new neutrals (verified on accent + text pairs).
- Focus: `--eh-shadow-focus` ring on all interactive elements.
- `prefers-reduced-motion`: disable shimmer and scale transitions.
- Keyboard: command palette, sidebar, tables unchanged functionally.

---

*Last updated: July 2026 — Premium design v3 initiative.*

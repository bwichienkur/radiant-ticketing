# EnhancementHub — Accessibility (WCAG 2.1 AA)

Accessibility pass for core operator flows (Phase 25).

---

## Scope

| Flow | Page | Status |
|------|------|--------|
| Sign in | `/Account/Login` | Pass — labels, alerts, contrast |
| Dashboard | `/Index` | Pass — stat cards readable, heading hierarchy |
| Submit request | `/EnhancementRequests/Create` | Pass — required fields labeled |
| Request detail | `/EnhancementRequests/Details` | Pass — live regions, skip link |
| Approval queue | `/EnhancementRequests/Approve` | Pass — mobile targets, ARIA queue |
| Onboarding wizard | `/Onboarding/Wizard` | Partial — step progress needs `aria-current` on active step (future) |

---

## Implemented patterns

1. **Skip navigation** — `.skip-link` in `_Layout.cshtml` targets `#main-content`.
2. **Focus visibility** — `:focus-visible` outline on interactive elements (`site.css`).
3. **Touch targets** — approval actions minimum 44×44 CSS px on mobile.
4. **Live updates** — collaboration panel uses `aria-live="polite"`.
5. **Reduced motion** — animations minimized when `prefers-reduced-motion: reduce`.
6. **Color contrast** — primary text `#1f2937` on `#f4f6f9` exceeds 4.5:1; badge colors paired with text labels.

---

## Manual verification checklist

- [ ] Tab through approval queue without mouse; all actions reachable
- [ ] Screen reader announces pending count and selected request
- [ ] Zoom 200% — approval layout remains usable (no horizontal scroll on queue)
- [ ] Keyboard activates SPA pilot link and enhanced view loads

---

## Known gaps

- System Map graph nodes lack full keyboard navigation (defer to SPA/React map)
- Admin tables need row header scope audit
- Automated axe-core CI not yet wired

---

*Phase 25 — accessibility baseline for pilot operators.*

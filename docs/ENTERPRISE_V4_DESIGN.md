# Enterprise Design System v4 — Dark First

**Direction:** Billion-dollar SaaS — Linear × Stripe × Vercel tier.  
**Default theme:** Dark (`#09090B` background). Light mode optional.

## Token palette (dark)

| Token | Value |
|-------|-------|
| `--eh-bg` | `#09090B` |
| `--eh-surface` | `#111827` |
| `--eh-surface-elevated` | `#1A2233` |
| `--eh-border` | `rgba(255,255,255,0.08)` |
| `--eh-primary` | `#6366F1` |
| `--eh-primary-hover` | `#7C83FF` |
| `--eh-accent` | `#8B5CF6` |
| `--eh-text` | `#F8FAFC` |
| `--eh-text-secondary` | `#94A3B8` |
| `--eh-muted` | `#64748B` |

## Spacing scale

4 · 8 · 12 · 16 · 24 · 32 · 48 · 64 (`--eh-space-1` … `--eh-space-8`)

## Shell

- **Header:** 48px sticky, backdrop blur, workspace selector, global search, AI, help, notifications, theme, user
- **Sidebar:** Section groups, collapsible, compact mode, active glow, Linear-inspired

## Components

Implemented in `eh-enterprise-v4.css` — inputs (44–48px), gradient buttons, elevated cards, enterprise tables, status pills, skeleton loaders.

## Implementation status

| Phase | Scope | Status |
|-------|-------|--------|
| 1 | Tokens + dark default | Done |
| 2 | Header + sidebar shell | Done |
| 3 | Forms, buttons, cards | Done |
| 4 | Tables, filters, badges | In progress |
| 5 | Per-page polish | Ongoing |

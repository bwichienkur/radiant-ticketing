# EnhancementHub Platform

Radiant Ticketing platform built on Next.js with event discovery, ticket purchasing, organizer dashboards, and QR check-in.

## Stack

- **Next.js 16** (App Router, API routes)
- **Prisma 7** + SQLite (development)
- **Tailwind CSS 4**
- **JWT** session cookies

## Getting started

```bash
npm install
npx prisma migrate dev
npm run db:seed
npm run dev
```

Open [http://localhost:3000](http://localhost:3000).

## Demo accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@enhancementhub.dev | password123 |
| Organizer | organizer@enhancementhub.dev | password123 |
| Customer | customer@enhancementhub.dev | password123 |

## Platform phases

See [docs/PHASES.md](docs/PHASES.md) for the full implementation roadmap.

| Phase | Scope | Status |
|-------|-------|--------|
| 1 | Foundation — scaffold, database, config | Complete |
| 2 | Auth — registration, login, roles | Complete |
| 3 | Events & venues — CRUD API | Complete |
| 4 | Commerce — orders, inventory, tickets | Complete |
| 5 | Customer portal — browse, purchase, my tickets | Complete |
| 6 | Admin dashboard — analytics, event management | Complete |
| 7 | Check-in & QR — validation workflow | Complete |

## API overview

- `POST /api/auth/register` — create account
- `POST /api/auth/login` — sign in
- `GET /api/events` — list published events
- `POST /api/orders` — purchase tickets
- `GET /api/tickets` — list user tickets
- `POST /api/checkin` — validate entry (organizer/admin)
- `GET /api/admin/stats` — dashboard metrics

## Environment

```env
DATABASE_URL="file:./dev.db"
JWT_SECRET="your-secret-key"
```

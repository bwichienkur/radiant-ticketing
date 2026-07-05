# EnhancementHub — Product Demo Video

**Video:** `/opt/cursor/artifacts/demo/enhancementhub-demo.mp4` (~87 seconds, 1280×720)  
**Subtitles:** `/opt/cursor/artifacts/demo/narration.srt`  
**Regenerate:** `node scripts/record-demo-video.mjs` (requires Web on `:5001`)

---

## What the demo covers

Each scene walks one core use case with on-screen narration.

| # | Use case | Route | What it shows |
|---|----------|-------|---------------|
| 1 | **Sign in** | `/Account/Login` | Role-based access; demo credentials for local dev |
| 2 | **Dashboard** | `/` | Control room: backlog stats, action queue, activity |
| 3 | **Submit request** | `/EnhancementRequests/Create` | Business intake form with priority and target app |
| 4 | **Request triage** | `/EnhancementRequests` | Search, filters, risk badges on the backlog |
| 5 | **Request detail** | `/EnhancementRequests/Details` | AI analysis, mission control, risk & impact |
| 6 | **React collaboration** | `/Spa/RequestDetail/{id}` | SignalR comments, presence, live analysis refresh |
| 7 | **System Map** | `/SystemMap/Index` | Code ↔ database graph by artifact type |
| 8 | **Cytoscape graph** | `/Spa/SystemMap` | Interactive zoom/pan graph with graph/list toggle |
| 9 | **Schema drift** | `/SchemaDrift` | Live DB vs code mismatch findings |
| 10 | **Approval queue** | `/EnhancementRequests/Approve` | Human approve / reject / clarify gate |
| 11 | **React approvals** | `/Spa/ApprovalQueue` | Mobile-friendly queue with J/K navigation |
| 12 | **Onboarding** | `/Spa/OnboardingWizard` | App registration, code, DB, discovery flow |
| 13 | **Applications** | `/Applications` | Portfolio of team-scoped applications |
| 14 | **Database connections** | `/DatabaseConnections` | Read-only schema registration for intelligence |
| 15 | **Background jobs** | `/Admin/Jobs` | Hangfire indexing, discovery, AI job health |
| 16 | **Tenancy & billing** | `/Admin/Tenancy` | Plans, Stripe, trial, schema-per-tenant isolation |
| 17 | **Compliance** | `/Admin/Compliance` | SOC 2 map, retention, audit for procurement |
| 18 | **Close** | `/` | End-to-end story: intake → analysis → approval → export |

---

## Narration script (matches video captions)

### 1. Sign in
Role-based access for submitters, approvers, and admins. SSO via Entra ID supported.

### 2. Dashboard control room
Backlog health at a glance: pending analysis, high-risk approvals, sparklines, and the action queue for your role.

### 3. Submit enhancement request
Capture business intent, priority, and target application — the foundation for grounded AI impact analysis.

### 4. Request triage
Search, filter, and sort the enhancement backlog. Risk badges highlight what needs attention first.

### 5. Request detail & AI analysis
Mission control shows risk, confidence, affected apps/repos, and DB recommendations from your indexed estate.

### 6. React detail + real-time collaboration
SignalR powers live comments, viewer presence, and analysis refresh on the React SPA hot path.

### 7. System Map
Graph links controllers, entities, services, and tables — architects see blast radius before approving.

### 8. Interactive Cytoscape graph
Zoom, pan, and select nodes by type. Graph/list toggle with a 400-node performance cap.

### 9. Schema drift detection
Scheduled scans compare live database schema against code mappings to catch mismatches early.

### 10. Approval queue
Human gate: approve, reject, or request clarification. Audit log records every decision.

### 11. React approval queue
Mobile-friendly queue with J/K keyboard navigation and quick decision actions.

### 12. Onboarding wizard
Register apps via local path, ZIP, GitHub App, or Git — then connect DB and run discovery.

### 13. Application portfolio
Team-scoped applications with linked repositories, DB connections, and intelligence profiles.

### 14. Database connections
Register read-only connections for schema scan, ERD export, and drift detection.

### 15. Background jobs
Hangfire orchestrates indexing, discovery, and AI analysis with retry and admin visibility.

### 16. Tenancy & billing
Multi-tenant plans, Stripe checkout, trial enforcement, and schema-per-tenant isolation.

### 17. Compliance & governance
SOC 2 readiness, retention policies, audit export, and security whitepaper for procurement.

### 18. Intake → analysis → approval → export
EnhancementHub deploys in your environment. Business request to Jira-ready ticket — with full audit trail.

---

## Prerequisites to re-record

```bash
# Services (from repo root)
docker compose up -d   # or dotnet run Web + Api + Worker

# Demo login
admin@enhancementhub.dev / password123

# Optional: set request ID used in detail scenes
export DEMO_REQUEST_ID=<guid>
node scripts/record-demo-video.mjs
```

See also [DEMO_SCRIPT.md](DEMO_SCRIPT.md) for a live 10-minute presenter walkthrough.

*July 2026 — automated product demo video.*

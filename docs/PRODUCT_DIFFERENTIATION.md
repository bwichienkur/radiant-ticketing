# EnhancementHub — Product Differentiation (Phase 24)

ROI metrics, approval policy engine, enhancement templates, and AI vs architect comparison.

---

## ROI dashboard

Admin-only metrics at `/Admin/Roi` and `GET /api/reporting/roi`.

| Metric | Description |
|--------|-------------|
| Analyses completed | Completed `AiPromptRun` records tied to analyses |
| Average analysis duration | Mean minutes from prompt start to completion |
| Estimated hours saved | `(4h manual baseline − actual) × completed analyses` |
| High/critical approved | Approved requests whose latest analysis was high or critical risk |
| Drift resolved | `SchemaDriftFinding` records with `IsResolved = true` |
| Architect edits | `ApprovalAction` entries with type `EditRequirements` |
| Human-approved findings | Analysis findings marked `IsHumanApproved` |

Resolve drift findings via `POST /api/schema-drift/findings/{id}/resolve` to feed the drift-resolved metric.

---

## Approval policy engine

Rules stored in `ApprovalPolicyRules` and evaluated when an approver submits **Approve**.

### Dimensions

- **Risk level** — latest analysis `RiskLevel` ≥ rule minimum
- **Department** — exact match on `EnhancementRequest.Department`
- **Application tier** — `Application.Tier` (`Standard`, `Critical`, `Low`)

When a rule matches and the approver's role is below `RequiredRole`, approval is blocked (`403 Forbidden`). **Admin** always bypasses policy checks.

### API (Admin)

| Method | Path |
|--------|------|
| GET | `/api/policies` |
| POST | `/api/policies` |
| DELETE | `/api/policies/{id}` |

### Seeded rules

1. Critical risk → Admin required  
2. Critical-tier application → Admin required  
3. Finance department → Admin required  

---

## Enhancement templates

Domain templates prefill intake for **Security**, **Performance**, and **Compliance**.

| Method | Path | Auth |
|--------|------|------|
| GET | `/api/templates?domainCategory=` | JWT |
| GET | `/api/templates/{id}` | JWT |

Web UI: template dropdown on **New Enhancement Request** (`/EnhancementRequests/Create`).

Create with template:

```json
POST /api/enhancementrequests
{
  "templateId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001",
  "title": "",
  "businessDescription": "",
  ...
}
```

Empty fields are filled from the template; `SupportingNotes` includes `Template: {category} | {name}`.

---

## AI vs architect comparison

Compare analysis versions and capture architect edits as a learning signal.

| Method | Path |
|--------|------|
| GET | `/api/analysis/{requestId}/compare?versionA=1&versionB=2` |
| POST | `/api/analysis/findings/{findingId}/approve` |
| POST | `/api/analysis/{requestId}/architect-edit` |

**Comparison** returns risk/confidence deltas, changed analysis fields, and finding add/remove/modify lists.

**Architect edit** updates analysis fields, sets `IsApprovedSnapshot`, and records an `EditRequirements` approval action with JSON before/after values.

**Finding approval** sets `IsHumanApproved` on individual AI-suggested findings.

Request detail page shows comparison automatically when two or more analysis versions exist.

---

## Application tier

Set `Application.Tier` when registering applications:

- `Standard` (default)
- `Critical` — triggers critical-tier approval policies
- `Low`

---

*Phase 24 — Horizon 4.2 product-led differentiation.*

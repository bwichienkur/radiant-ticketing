# FinOps & Infrastructure Cost Constraints — Product Roadmap

**Status:** Draft (July 2026)  
**Related:** [INTAKE_COPILOT_ROADMAP.md](INTAKE_COPILOT_ROADMAP.md), [ROADMAP.md](ROADMAP.md)

## Problem

EnhancementHub AI analysis and refactor planning today produce **technical** recommendations — impacted areas, migration steps, developer effort hours — but do **not** consider whether those changes imply new cloud spend (Azure Functions, queues, SaaS APIs, GPU inference, egress, storage tiers).

Customers increasingly expect **shift-left FinOps**: cost evaluated alongside latency, resilience, and compliance *before* architecture is approved — not discovered on the monthly cloud bill.

### Current gap (baseline)

| Area | Today |
|------|-------|
| `OpenAiAnalysisService` prompt | summary, impactedAreas, recommendations, risks, estimatedEffortHours |
| `RefactorPlanGeneratorService` prompt | SQL migration steps, technical riskLevel |
| `EnhancementRequest` fields | No deployment target, budget band, or cost sensitivity |
| `ApplicationProfile.DeploymentNotes` | Column exists; **never populated or sent to AI** |
| Risk scoring | Effort hours + impacted areas — **not dollars** |
| Approval policies | Risk, department, tier — **no cost threshold** |
| Intake Copilot | Drafts business fields — **no infra constraints** |

What *is* cost-aware today is **EnhancementHub's own LLM spend** (`AI:Budget`, `AiPromptRun.EstimatedCostUsd`) and **SaaS plan limits** (`CommercialOptions`) — not customer infrastructure.

## Solution

Treat **customer infrastructure economics** as a first-class constraint in the analysis pipeline:

```
App deployment profile (defaults)
  + Request-level overrides (intake / create form)
  → Injected into AI prompts (analysis, refactor, intake)
  → Structured cost impact in output
  → Optional approval gate for high incremental spend
```

### Design principle: hybrid model

| Layer | Mechanism | Purpose |
|-------|-----------|---------|
| **Application defaults** | Configuration (onboarding + app settings) | Stable org standards: cloud, hosting model, cost sensitivity |
| **Request overrides** | Create Request form + Intake Copilot follow-up | One-off constraints: “no new SaaS this quarter”, initiative budget |
| **AI output** | Structured JSON fields on analysis/refactor | Auditable cost implications + alternatives |
| **Governance** | Approval policy extensions | High-cost proposals require extra approver |

**Do not rely on prompt alone** — structured fields prevent hallucinated dollar amounts and skipped constraints. **Do not rely on config alone** — intake must capture exceptions.

---

## Phases

| Phase | Scope | Status |
|-------|--------|--------|
| **A — Prompt grounding** | Wire existing profile fields; extend analysis/refactor prompts with cost-awareness instructions | Done |
| **B — Structured preferences** | `ApplicationInfrastructureProfile` + optional request constraints | Planned |
| **C — Cost impact output** | `infrastructureImplications` on analysis; UI on Request Detail | Planned |
| **D — Governance** | Approval rules by cost risk; FinOps dashboard slice | Planned |
| **E — FinOps integration** | Read-only Azure Cost Management / AWS Cost Explorer (enterprise) | Future |

---

## Phase A — Prompt grounding (quick win)

**Goal:** AI sees deployment context without new UI entities.

### Backend

| Change | Location |
|--------|----------|
| Populate `DeploymentNotes` during onboarding (free text or dropdowns) | `CreateApplicationCommand`, onboarding wizard |
| Pass `DeploymentNotes`, `DatabaseUsage`, `ExternalIntegrations` to analysis | `AiAnalysisJobExecutor`, `TriggerAiAnalysisCommand`, `PromptSanitizer.BuildStructuredPrompt` |
| Same context in refactor plan user prompt | `RefactorPlanGeneratorService` |
| Include deployment notes in Intake Copilot application context | `IntakeCopilotService.BuildUserPrompt` |

### Prompt additions (analysis)

```
When recommendations imply new cloud services, external APIs, or hosting changes:
- List each implied service with purpose
- Suggest at least one lower-cost alternative where feasible
- Flag costRiskLevel if incremental spend may be material
Honor any infrastructure constraints in the user prompt.
```

### Success criteria

- Demo request with “must stay on existing App Service” yields alternatives to Azure Functions
- `DeploymentNotes` visible in `AiPromptRun` user prompt audit (redacted)

---

## Phase B — Structured preferences

**Goal:** Repeatable, queryable constraints — not only free text.

### Domain model (proposed)

**`ApplicationInfrastructureProfile`** (1:1 with `Application`, or JSON column on `Application`):

| Field | Type | Example |
|-------|------|---------|
| `CloudProvider` | enum | Azure, AWS, GCP, Hybrid, OnPrem |
| `PrimaryHostingModel` | enum | AppService, Functions, AKS, Lambda, VMs, Mixed |
| `CostSensitivity` | enum | CostOptimized, Balanced, PerformanceFirst |
| `MonthlyBudgetBand` | enum | Under500, 500To2000, 2000To10000, Over10000, Unknown |
| `NewServicesPolicy` | enum | AllowWithApproval, DiscouragePaidSaaS, ServerlessOnly, NoNewServices |
| `ExistingServicesJson` | JSON | `["Service Bus", "Cosmos DB", "Redis"]` |
| `Notes` | string | Free-form constraints |

**`EnhancementRequest`** (optional per-request overrides):

| Field | Type | Example |
|-------|------|---------|
| `InfrastructureConstraints` | string | “No new vendors; stay within App Service plan” |
| `IncrementalBudgetUsd` | decimal? | 200 (nullable — initiative cap) |

### UI touchpoints

| Surface | Interaction |
|---------|-------------|
| **Onboarding wizard** | Step “Deployment & cost” — cloud, hosting, budget band (skippable) |
| **Application detail** | Edit infrastructure profile |
| **Create Request** | Collapsible “Infrastructure constraints” (pre-filled from app defaults) |
| **Intake Copilot** | Follow-up when draft implies async/webhooks/ML/external APIs: “Budget band or hosting preference?” |

### API

| Method | Route | Purpose |
|--------|-------|---------|
| GET/PUT | `/web-api/spa/applications/{id}/infrastructure-profile` | App defaults |
| — | Extend create-request / intake draft DTOs | Request overrides |

---

## Phase C — Cost impact output

**Goal:** Approval-ready cost section, not buried in prose recommendations.

### Extended analysis JSON schema

```json
{
  "summary": "...",
  "impactedAreas": ["..."],
  "recommendations": ["..."],
  "risks": ["..."],
  "estimatedEffortHours": 24,
  "costRiskLevel": "Low|Medium|High",
  "infrastructureImplications": [
    {
      "service": "Azure Functions (Consumption)",
      "purpose": "Async cancellation webhook",
      "estimatedMonthlyUsdLow": 20,
      "estimatedMonthlyUsdHigh": 80,
      "alternatives": ["Hangfire on existing Worker", "Azure Logic Apps"]
    }
  ],
  "costNotes": "Egress to external compliance API — verify data transfer pricing"
}
```

### Persistence & UI

| Layer | Change |
|-------|--------|
| Domain | `EnhancementAnalysis.CostRiskLevel`, `InfrastructureImplicationsJson` (or child table) |
| Request Detail SPA | “Infrastructure & cost impact” panel with alternatives |
| Export / ticket | Include cost section in Jira/ADO export payload |

### Disclaimer

Estimates are **order-of-magnitude guidance** for architecture review — not billing forecasts. Link to customer FinOps tools when integrated (Phase E).

---

## Phase D — Governance

**Goal:** High incremental cost triggers the same discipline as high technical risk.

### Approval policy extensions

Extend `ApprovalPolicyRule`:

| Field | Purpose |
|-------|---------|
| `MinimumCostRiskLevel` | Require VP approval when `costRiskLevel >= High` |
| `MaxEstimatedIncrementalUsd` | Optional numeric gate (when request budget provided) |

### Reporting

| Metric | Source |
|--------|--------|
| % analyses with non-empty `infrastructureImplications` | `EnhancementAnalysis` |
| High cost-risk approval cycle time | Approval workflow |
| Requests where cheapest alternative was chosen (manual tag) | Future feedback loop |

---

## Phase E — FinOps integration (enterprise)

**Goal:** Ground estimates in actual spend — not LLM guesses alone.

| Integration | Data |
|-------------|------|
| Azure Cost Management + Resource Graph | Historical spend by resource type, subscription |
| AWS Cost Explorer | Same for AWS estates |
| FOCUS-aligned export | Normalize multi-cloud cost attribution |

Read-only, customer-consented. Used to calibrate `estimatedMonthlyUsd` ranges and flag “you already pay for X — reuse it.”

---

## Architecture

```
┌─────────────────────┐     ┌──────────────────────────┐
│ Application         │     │ EnhancementRequest       │
│ InfrastructureProfile│    │ + optional constraints   │
└─────────┬───────────┘     └────────────┬─────────────┘
          │                              │
          └──────────────┬───────────────┘
                         ▼
              ┌─────────────────────┐
              │ ConstraintBundle    │
              │ (merged for prompt) │
              └─────────┬───────────┘
                        │
     ┌──────────────────┼──────────────────┐
     ▼                  ▼                  ▼
OpenAiAnalysis   RefactorPlanGenerator   IntakeCopilot
     │                  │                  │
     └──────────────────┴──────────────────┘
                        ▼
              EnhancementAnalysis
              + infrastructureImplications
                        ▼
              Request Detail UI + Approval
```

| Layer | Component |
|-------|-----------|
| Domain | `ApplicationInfrastructureProfile`, analysis cost fields |
| Application | Commands/queries for profile CRUD; merge constraints in analysis handlers |
| Infrastructure | Prompt builders, schema validation for cost JSON |
| Web BFF | SPA endpoints for profile + extended create-request |
| UI | Onboarding step, Application settings, Create Request constraints, Request Detail cost panel |

---

## Guardrails

- Cost output is **advisory** — human approver remains accountable
- Never auto-approve based on cost estimate alone
- PII redaction applies to deployment notes and constraints before LLM calls
- `AiPromptRun` audit trail includes constraint snapshot (hashed or redacted)
- Mock/offline analysis returns placeholder cost section when AI not configured

---

## Out of scope

- Real-time cloud billing enforcement (customer’s FinOps tooling owns that)
- Automatic rightsizing of deployed resources
- Customer LLM hosting decisions (see response on provider strategy — API vs self-host)

---

## Success metrics

- Reduction in post-approval “surprise infra cost” feedback from design partners
- % of high cost-risk analyses reviewed before implementation starts
- Time to complete approval when `costRiskLevel = High` (should decrease with clearer packages)
- Correlation between stated budget band and recommended alternatives selected

---

## Implementation order (recommended)

1. **Phase A** — lowest effort; unblocks demos with deployment notes in prompts  
2. **Phase B** — structured preferences + intake follow-up  
3. **Phase C** — structured output + Request Detail UI (highest user-visible value)  
4. **Phase D** — governance once cost fields are reliable  
5. **Phase E** — enterprise differentiator after 2+ pilot customers on Azure

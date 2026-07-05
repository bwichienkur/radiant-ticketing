# Intake Copilot — Product Roadmap

Grounded, conversational intake that turns natural-language intent into structured `EnhancementRequest` records — separate from Pipeline search (keyword navigation) and post-submit analysis.

## Problem

The create-request form assumes users can articulate title, business description, and desired outcome. Most cannot — especially when they think in outcomes (“we need SOC 2 access logging”) rather than system terms. Intake quality is the bottleneck before AI analysis runs.

## Solution

**Intake Copilot**: a scoped assistant on Create Request that interviews the user against indexed applications and knowledge, then produces a reviewable draft request.

```
Describe intent → (optional multi-turn Q&A) → Draft request → User edits → Submit → Existing analysis pipeline
```

## Phases

| Phase | Scope | Status |
|-------|--------|--------|
| **1 — Quick draft** | Single prompt → AI pre-fills form fields + suggests template | Done |
| **2 — Interactive intake** | Multi-turn session (max 5 turns), repo-aware follow-up questions, draft stored until confirm | Done |
| **3 — Deep grounding** | Knowledge search + application profiles in context; Slack/Teams intake uses same engine | Done |
| **4 — Policy intake** | PDF/TXT/MD upload + HTTPS URL fetch → compliance-oriented draft | Done |
| **5 — Provenance & submit** | Session-linked submit with form overrides; policy source on request; mock Compliance template | Done |

## Architecture

| Layer | Component |
|-------|-----------|
| Domain | `IntakeCopilotSession`, `AiWorkflowStep.IntakeCopilot` |
| Application | MediatR commands/queries, `IIntakeCopilotService` |
| Infrastructure | `IntakeCopilotService`, `ChatIntakeService` upgrade |
| Web BFF | `SpaIntakeController` — `/web-api/spa/intake/*` |
| UI | `IntakeCopilotPanel` on `/Spa/CreateRequest` |

## API

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/web-api/spa/intake/sessions` | Start session (optional initial prompt) |
| GET | `/web-api/spa/intake/sessions/{id}` | Session state + messages + draft |
| POST | `/web-api/spa/intake/sessions/{id}/messages` | Send user message (multi-turn) |
| POST | `/web-api/spa/intake/sessions/{id}/policy-document` | Attach policy PDF/TXT/MD (multipart) |
| POST | `/web-api/spa/intake/sessions/{id}/policy-url` | Fetch policy from HTTPS URL |
| POST | `/web-api/spa/intake/sessions/{id}/create-request` | Finalize → `EnhancementRequest` (optional form overrides in body) |

## Guardrails

- Draft always requires human review before submit
- Max 5 assistant turns per session
- PII redaction via existing `IPiiRedactionService`
- `AiPromptRun` audit trail for each LLM call
- UX disclaimer: grounded intake assistant, not legal/compliance advice
- Pipeline search remains non-generative

## Out of scope (future)

- Auto-submit without user confirm
- General-purpose dashboard chat

See `docs/POLICY_INTAKE.md` for policy document extraction details.

Phase 5 links intake sessions to submitted requests: when the user started via Intake Copilot, submit calls `create-request` with edited form values and appends `Policy source: …` to supporting notes when a policy was attached.

## Success metrics

- % of create-request flows using Intake Copilot
- Reduction in `NeedsClarification` rate post-submit
- Time from landing on Create Request to submit

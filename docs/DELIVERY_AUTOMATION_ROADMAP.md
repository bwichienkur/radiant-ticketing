# Delivery automation roadmap

Automated implementation, test deployment, QA evidence, requester UAT, and production scheduling — configurable per **tenant** (company) and per **application**.

## Vision

After business approval, EnhancementHub orchestrates:

1. **Implement** — AI creates a feature branch and opens a pull request (never silent push to `main`).
2. **Deploy to test** — Customer CI/CD or cloud adapter deploys to the tenant’s Test environment.
3. **QA** — Automated tests run against the test URL; step list + video artifact stored on the request.
4. **UAT** — Original requester signs off on behavior in test.
5. **Production** — Deploy scheduled per change window; prod uses app-specific mechanism and environment config.

EnhancementHub is the **governance and orchestration hub**. Deploy **execution** stays in customer-controlled pipelines (GitHub Actions, Azure DevOps, App Service, Kubernetes, webhooks).

## Configuration model (three layers)

### Tenant — where things run

| Entity | Purpose |
|--------|---------|
| `TenantDeliveryProfile` | Default CI/CD provider, vault prefix, automation policies, change window notes |
| `TenantDeploymentEnvironment` | Named environments (Test, UAT, Staging, Production) with URL templates and secret ref prefixes |

Secrets are **references** (`kv://…`, `secret://…`), never raw connection strings in EnhancementHub.

### Application — how this app deploys

| Entity | Purpose |
|--------|---------|
| `ApplicationDeliveryProfile` | Mechanism (App Service, K8s, Functions, …), primary repo, branch pattern, pipeline ref |
| `ConfigTransformsJson` | Per-environment appsettings merges and env vars |
| `ConnectionMappingsJson` | Logical connection → secret ref per environment type |

### Request — what happened (Phase B+)

| Entity (planned) | Purpose |
|------------------|---------|
| `EnhancementDeliveryRun` | Branch, PR, deploy IDs, QA run, video URL, UAT sign-off, prod schedule |

## End-to-end status flow (planned)

```text
Approved → Implementing → InTest → QaInProgress → AwaitingUat
  → UatApproved → ProdScheduled → DeployingToProduction → Completed
```

Gates: approval → QA evidence → requester UAT → change window (optional second approver).

## Phases

### Phase A — Delivery profiles (shipped in this branch)

**Goal:** Store tenant and application delivery configuration; validate without executing deploys.

| Deliverable | Status |
|-------------|--------|
| `TenantDeliveryProfile`, `TenantDeploymentEnvironment`, `ApplicationDeliveryProfile` entities | Done |
| EF migration `Phase41DeliveryAutomationPhaseA` | Done |
| MediatR queries/commands + `DeliveryProfileValidator` dry-run | Done |
| Admin UI `/Admin/Delivery` | Done |
| BFF `web-api/spa/delivery/*` | Done |
| Demo seed for Radiant Commerce Platform | Done |
| Structural tests `DeliveryAutomationPhaseATests` | Done |

**Exit criteria:** Admin can define Test/Prod environments and per-app pipeline refs; validation reports missing config.

### Phase B — Implement on approval (branch + PR)

**Goal:** On `Approved` (or manual trigger), code agent creates branch + PR from analysis/refactor plan.

| Deliverable | Notes |
|-------------|-------|
| `EnhancementDeliveryRun` entity + status enum extension | Versioned runs per request |
| `IDeliveryOrchestrationService` + Hangfire job | Idempotent steps |
| GitHub App: create branch, commit, open PR | Extend existing repo integration |
| Policy: `RequirePullRequestReview` from tenant profile | No auto-merge in v1 |
| Request detail UI: implementation timeline | Link to PR |

**Exit criteria:** Approved demo request gets a linked PR; audit log records branch name.

### Phase C — Deploy to test (adapter pattern)

**Goal:** Invoke customer CI/CD to deploy to Test environment.

| Deliverable | Notes |
|-------------|-------|
| `IDeploymentAdapter` interface | `DeployAsync(DeploymentContext)` |
| `GitHubActionsDeploymentAdapter` | `workflow_dispatch` with env + config bundle |
| `WebhookDeploymentAdapter` | Generic POST for custom CI |
| Config bundle builder | Merges transforms + vault-resolved connection refs for target env |
| Azure App Service adapter (optional) | Slot deploy for .NET ICP |

**Exit criteria:** Test deploy triggered via GitHub Actions sample workflow; `testUrl` stored on delivery run.

### Phase D — Automated QA + evidence

**Goal:** Playwright tests from analysis `TestingPlan`; store step list + video.

| Deliverable | Notes |
|-------------|-------|
| QA script generator from analysis | Structured steps |
| Playwright runner in Worker | `recordVideo` against `testUrl` |
| Artifact store (S3/blob) | Tenant-scoped retention from `QaVideoRetentionDays` |
| Request UI: QA report + video | Before UAT |

**Exit criteria:** Demo request shows pass/fail steps and playable video after test deploy.

### Phase E — UAT + production scheduling

**Goal:** Requester sign-off; prod deploy respects change windows.

| Deliverable | Notes |
|-------------|-------|
| UAT portal for requester | Checklist from `desiredOutcome` |
| `RequireUatSignoff` enforcement | Blocks prod until signed |
| Change window scheduler | `ProdScheduled` until window opens |
| Prod deploy via same adapters | `environmentType = Production` |
| Optional re-approval for prod | Tenant policy |

**Exit criteria:** Full demo path from approval → test → QA → UAT → scheduled prod (sandbox).

## Adapter matrix (target)

| Adapter | Test | Production | Config injection |
|---------|------|------------|------------------|
| GitHub Actions | `workflow_dispatch` | Environment protection rules | Repo secrets + env |
| Azure DevOps | Pipeline variables | Approval gates | Variable groups |
| Azure App Service | Deploy to staging slot | Slot swap | App settings + conn strings |
| Kubernetes/Argo | Sync test namespace | Sync prod | Helm values per env |
| Webhook | POST payload | Same + `targetEnv` | Customer-owned |

## Example connection mappings (JSON on application profile)

```json
{
  "mappings": [
    {
      "logicalName": "DefaultConnection",
      "byEnvironment": {
        "Test": "kv://contoso/orders-db-test",
        "Uat": "kv://contoso/orders-db-uat",
        "Production": "kv://contoso/orders-db-prod"
      }
    }
  ]
}
```

## Example config transforms (JSON)

```json
{
  "Test": {
    "appsettings": {
      "FeatureFlags:NewCancelFlow": true
    },
    "env": {
      "ASPNETCORE_ENVIRONMENT": "Staging"
    }
  },
  "Production": {
    "appsettings": {
      "FeatureFlags:NewCancelFlow": true
    }
  }
}
```

## Safety principles

- Never auto-deploy production without UAT (when `RequireUatSignoff` is on).
- Migrations run only against the **target environment** DB ref — never prod by mistake.
- All stages write to audit log + `EnhancementDeliveryRun` timeline.
- v1: single primary repo per request; multi-repo coordination in a later phase.

## API reference (Phase A)

| Method | Route | Role |
|--------|-------|------|
| GET | `/web-api/spa/delivery/tenant-profile` | Admin |
| PUT | `/web-api/spa/delivery/tenant-profile` | Admin |
| POST | `/web-api/spa/delivery/tenant-profile/validate` | Admin |
| POST | `/web-api/spa/delivery/tenant-environments` | Admin |
| DELETE | `/web-api/spa/delivery/tenant-environments/{id}` | Admin |
| GET | `/web-api/spa/delivery/applications/{id}/profile` | Authenticated |
| PUT | `/web-api/spa/delivery/applications/{id}/profile` | Authenticated |
| POST | `/web-api/spa/delivery/applications/{id}/profile/validate` | Authenticated |

## Related docs

- [ROADMAP.md](ROADMAP.md) — Horizon 4.6
- [FINOPS_INFRASTRUCTURE_ROADMAP.md](FINOPS_INFRASTRUCTURE_ROADMAP.md) — cost constraints in analysis
- [DEPLOYMENT.md](DEPLOYMENT.md) — running EnhancementHub itself

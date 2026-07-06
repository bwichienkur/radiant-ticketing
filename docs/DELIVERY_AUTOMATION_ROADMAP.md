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

| Entity | Purpose |
|--------|---------|
| `EnhancementDeliveryRun` | Branch, PR, deploy IDs, QA run, video URL, UAT sign-off, prod schedule |

## End-to-end status flow (shipped)

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

| Deliverable | Status |
|-------------|--------|
| `EnhancementDeliveryRun` entity + status enum extension | Done |
| `IDeliveryOrchestrationService` + Hangfire job | Done |
| GitHub App: create branch, commit, open PR | Done (simulated when App not configured) |
| Policy: `RequirePullRequestReview` from tenant profile | Done |
| Request detail UI: implementation timeline | Done (`DeliveryRunPanel`) |

**Exit criteria:** Approved demo request gets a linked PR; audit log records branch name.

### Phase C — Deploy to test (adapter pattern)

**Goal:** Invoke customer CI/CD to deploy to Test environment.

| Deliverable | Status |
|-------------|--------|
| `IDeploymentAdapter` interface | Done |
| `GitHubActionsDeploymentAdapter` | Done (`workflow_dispatch` + simulation fallback) |
| `WebhookDeploymentAdapter` | Done |
| Config bundle builder | Done |
| Azure App Service adapter (optional) | Deferred |

**Exit criteria:** Test deploy triggered via GitHub Actions sample workflow; `testUrl` stored on delivery run.

### Phase D — Automated QA + evidence

**Goal:** Playwright tests from analysis `TestingPlan`; store step list + video.

| Deliverable | Status |
|-------------|--------|
| QA script generator from analysis | Done (structured steps from testing plan) |
| Playwright runner in Worker | Simulated HTML walkthrough artifact (real Playwright in follow-up) |
| Artifact store (S3/blob) | Done via `IFileStorageService` (local/S3) |
| Request UI: QA report + video | Done |

**Exit criteria:** Demo request shows pass/fail steps and playable video after test deploy.

### Phase E — UAT + production scheduling

**Goal:** Requester sign-off; prod deploy respects change windows.

| Deliverable | Status |
|-------------|--------|
| UAT portal for requester | Done (Request Detail `DeliveryRunPanel`) |
| `RequireUatSignoff` enforcement | Done |
| Change window scheduler | Done (`ChangeWindowEvaluator`) |
| Prod deploy via same adapters | Done |
| Optional re-approval for prod | Tenant policy hooks ready |

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

## API reference (Phase A–E)

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
| GET | `/web-api/spa/delivery/requests/{requestId}/run` | Authenticated |
| POST | `/web-api/spa/delivery/requests/{requestId}/start` | Authenticated |
| POST | `/web-api/spa/delivery/requests/{requestId}/advance-pr` | Authenticated |
| POST | `/web-api/spa/delivery/requests/{requestId}/uat` | Authenticated |
| GET | `/web-api/spa/delivery/requests/{requestId}/artifacts/{kind}` | Authenticated |

## Test catalog (Phase D+)

Regression test cases are stored per application and executed as part of each delivery QA run.

### Entities

| Entity | Purpose |
|--------|---------|
| `ApplicationTestSuite` | Named suite per application (default: Regression) |
| `TestCase` | Structured steps, status (Draft/Active/Retired), origin (AI/Manual/Promoted) |
| `TestCaseVersion` | Immutable snapshot executed for a given run |
| `DeliveryRunTestResult` | Per-case pass/fail, duration, and artifact paths for one delivery run |

### Flow

1. **Analysis** produces `TestingPlan` text.
2. **Before QA**, `ITestCaseCatalogService.PrepareQaRunAsync` syncs draft cases for the request and builds a manifest: active regression cases + new drafts.
3. **`IQaRunner`** executes the manifest (`SimulatedQaRunner` today; Playwright worker next).
4. Results are stored on `EnhancementDeliveryRun` and `DeliveryRunTestResults`.
5. **On QA pass**, draft cases that passed are promoted to `Active` regression cases.

### API

| Method | Route |
|--------|-------|
| GET | `/web-api/spa/delivery/applications/{applicationId}/test-suite` |
| GET | `/web-api/spa/delivery/applications/{applicationId}/regression-runs` |

### QA runners

| Runner | Config | Behavior |
|--------|--------|----------|
| `PlaywrightQaRunner` (default) | `Delivery:Qa:Runner=Playwright` | HTTP validation against `testUrl`; optional browser when `Delivery:Qa:PlaywrightBrowserEnabled=true` |
| `SimulatedQaRunner` | `Delivery:Qa:Runner=Simulated` | Always simulated artifacts |

On PR creation, draft test cases export to `tests/e2e/eh-{requestId}.spec.ts` on the feature branch.

Nightly Hangfire job `nightly-regression` (03:00 UTC) runs active regression cases per application against the tenant Test environment URL.

## CI/CD hardening (Phase F)

| Feature | Description |
|---------|-------------|
| One-click production deploy | Gated button when `RequiresHumanProdDeploy` and QA/UAT pass |
| One-click production rollback | Dispatches rollback workflow with previous `commitSha` / deploy ref |
| Artifact promotion | Production deploy passes `artifactRef` + `promoteFromEnvironment=Test` |
| Post-deploy smoke | Regression manifest re-run against production URL after deploy |
| Tenant policies | `AllowOneClickProdDeploy`, `AllowOneClickRollback`, `TestDataStrategy`, `AllowProdToTestRefresh` |

### API

| Method | Route |
|--------|-------|
| POST | `/web-api/spa/delivery/requests/{requestId}/deploy-production` |
| POST | `/web-api/spa/delivery/requests/{requestId}/rollback-production` |

## Related docs

- [ROADMAP.md](ROADMAP.md) — Horizon 4.6
- [FINOPS_INFRASTRUCTURE_ROADMAP.md](FINOPS_INFRASTRUCTURE_ROADMAP.md) — cost constraints in analysis
- [DEPLOYMENT.md](DEPLOYMENT.md) — running EnhancementHub itself

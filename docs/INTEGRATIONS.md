# EnhancementHub — Integrations (Phase 23)

Polyglot symbol ingestion, OpenAPI registration, GitHub push webhooks, chat intake, and optional ServiceNow sync.

Configuration lives under the `Integrations` section in `appsettings.json`.

---

## OpenAPI / Swagger ingestion

Register OpenAPI 3 JSON specs per application. Endpoints are parsed from `paths` and stored for system map and analysis context.

### API

| Method | Path | Auth |
|--------|------|------|
| `POST` | `/api/integrations/openapi` | JWT |
| `GET` | `/api/integrations/openapi?applicationId={id}` | JWT |
| `GET` | `/api/integrations/openapi/{registrationId}/endpoints` | JWT |

Example registration body:

```json
{
  "applicationId": "00000000-0000-0000-0000-000000000001",
  "name": "Orders API",
  "specDocument": "{ \"openapi\": \"3.0.1\", \"paths\": { ... } }"
}
```

The service extracts `servers[0].url` as `BaseUrl` and stores each path/method pair as an `OpenApiEndpoint`.

---

## Polyglot symbol ingestion

Partner-style ingestion for Java, Python, TypeScript, and JavaScript symbols (no in-process tree-sitter). External scanners or CI pipelines POST symbol metadata; EnhancementHub stores them in the same `IndexedFile` / `IndexedSymbol` tables used by Roslyn indexing.

### API

`POST /api/integrations/polyglot/symbols` — **Admin** or **Developer** role required.

```json
{
  "repositoryId": "00000000-0000-0000-0000-000000000002",
  "language": "java",
  "symbols": [
    {
      "filePath": "src/main/java/com/example/OrderService.java",
      "symbolName": "OrderService",
      "symbolKind": "class",
      "summary": "Handles order lifecycle",
      "lineStart": 10,
      "lineEnd": 120
    }
  ]
}
```

Enable via:

```json
"Integrations": {
  "Polyglot": {
    "Enabled": true,
    "SupportedLanguages": [ "java", "python", "typescript", "javascript" ]
  }
}
```

---

## GitHub push webhooks

When enabled, `POST /api/webhooks/github` accepts GitHub `push` events, verifies `X-Hub-Signature-256`, matches repositories by URL (`org/repo`), and queues indexing for repos with `AutoIndexOnPush = true` (default).

```json
"Integrations": {
  "GitHub": {
    "Enabled": true,
    "WebhookSecret": "your-webhook-secret"
  }
}
```

Configure the GitHub App or repository webhook to send **push** events to:

`https://<host>/api/webhooks/github`

---

## Slack & Microsoft Teams intake

Submit enhancement requests from chat using pipe-delimited text: `Title | Description`.

### Slack

`POST /api/integrations/slack/intake` (anonymous when Slack intake is enabled)

```json
{
  "text": "Mobile login fix | Users cannot authenticate on iOS",
  "userName": "jane.doe",
  "channelName": "product-requests"
}
```

```json
"Integrations": {
  "Slack": {
    "Enabled": true,
    "SigningSecret": "",
    "DefaultPriority": "Medium"
  }
}
```

### Teams

`POST /api/integrations/teams/intake` with header `X-EnhancementHub-Intake-Key`.

```json
{
  "text": "Export feature | Add CSV export for reports",
  "userName": "teams-user",
  "targetApplicationId": null,
  "teamId": null
}
```

```json
"Integrations": {
  "Teams": {
    "Enabled": true,
    "IntakeSecret": "shared-secret",
    "DefaultPriority": "Medium"
  }
}
```

Intake creates a **Submitted** enhancement request and a synthetic integration user (`*@integrations.enhancementhub.local`).

---

## ServiceNow (optional)

### Outbound export

When configured, EnhancementHub can export approved change packages to ServiceNow via `ServiceNowTicketExporter` (`ExternalTicketProvider.ServiceNow`).

```json
"Integrations": {
  "ServiceNow": {
    "Enabled": true,
    "InstanceUrl": "https://instance.service-now.com",
    "Username": "integration-user",
    "Password": "secret",
    "TableName": "change_request",
    "WebhookSecret": "inbound-secret"
  }
}
```

### Inbound status sync

`POST /api/integrations/servicenow/webhook` with header `X-ServiceNow-Webhook-Secret` updates linked enhancement request status from ServiceNow state changes.

```json
{
  "externalId": "CHG0012345",
  "state": "approved",
  "shortDescription": "Optional title update"
}
```

Mapped states include `approved`, `rejected`, `implement`, `closed`, and `cancelled`.

---

## Repository auto-index flag

`Repository.AutoIndexOnPush` (default `true`) controls whether GitHub push webhooks queue re-indexing for that repository. Disable for large monorepos that rely on scheduled indexing only.

---

## Security notes

- OpenAPI and polyglot endpoints require authenticated users with application or role access.
- Webhook and chat intake endpoints are anonymous but gated by shared secrets or feature flags.
- Leave webhook secrets empty only in local development; production must set secrets explicitly.

---

*Phase 23 — Horizon 4.1 polyglot & integration expansion.*

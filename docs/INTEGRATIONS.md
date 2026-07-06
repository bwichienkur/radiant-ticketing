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

## Outbound customer webhooks (Phase 52)

Register HTTPS endpoints in **Admin → Webhooks** to receive signed JSON POSTs when workflow events occur.

### Supported events

| Event type | Trigger |
|------------|---------|
| `request.approved` | Enhancement request is approved |
| `analysis.completed` | AI analysis finishes and request moves to pending approval |
| `drift.detected` | Schema drift scan completes |

### Subscription setup

1. Open `/Admin/Webhooks` as an Admin user.
2. Enter endpoint URL and select event types.
3. Copy the signing secret when shown — it is displayed only once.

### Delivery format

```http
POST https://your-endpoint.example/hooks/enhancementhub
Content-Type: application/json
X-EnhancementHub-Signature: t=1710000000,v1=<hmac_sha256_hex>
User-Agent: EnhancementHub/1.0
```

```json
{
  "eventType": "request.approved",
  "timestamp": "2026-07-06T15:00:00Z",
  "data": {
    "enhancementRequestId": "279c38dc-8da4-400b-828f-711726210eb6",
    "title": "Add order cancellation reason for compliance",
    "approvedByUserId": "11111111-1111-1111-1111-111111111111",
    "approvedAt": "2026-07-06T15:00:00Z",
    "status": "Approved"
  }
}
```

### Signature verification (Zapier / custom receiver)

1. Read the raw request body as a string (before JSON parsing).
2. Parse `X-EnhancementHub-Signature` header: `t=<unix_seconds>,v1=<signature>`.
3. Compute HMAC-SHA256 of `{timestamp}.{raw_body}` using your webhook signing secret.
4. Compare the hex digest to `v1` using a constant-time comparison.

Example pseudo-code:

```text
signed_payload = timestamp + "." + raw_body
expected = HMAC_SHA256_HEX(secret, signed_payload)
assert constant_time_equals(expected, signature_from_header)
```

### Retry policy

Failed deliveries (non-2xx HTTP or network error) retry up to **5 attempts** with exponential backoff. Delivery status is visible in the **Recent deliveries** table on `/Admin/Webhooks`.

### Analysis completed payload

```json
{
  "eventType": "analysis.completed",
  "timestamp": "2026-07-06T15:00:00Z",
  "data": {
    "enhancementRequestId": "279c38dc-8da4-400b-828f-711726210eb6",
    "title": "Add order cancellation reason for compliance",
    "analysisId": "66666666-6666-6666-6666-666666666666",
    "analysisVersion": 1,
    "status": "PendingApproval",
    "completedAt": "2026-07-06T15:00:00Z"
  }
}
```

### Drift detected payload

```json
{
  "eventType": "drift.detected",
  "timestamp": "2026-07-06T15:00:00Z",
  "data": {
    "databaseConnectionId": "55555555-5555-5555-5555-555555555555",
    "connectionName": "EnhancementHub SQLite",
    "applicationId": "33333333-3333-3333-3333-333333333333",
    "findingCount": 3,
    "criticalCount": 1,
    "detectedAt": "2026-07-06T15:00:00Z"
  }
}
```

---

*Phase 52 — Outbound webhooks & workflow automation MVP.*


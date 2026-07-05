# Policy Intake — Compliance Document → Enhancement Draft

Phase 4 of Intake Copilot: attach a compliance or policy document and let the copilot draft a structured enhancement request oriented toward closing policy gaps.

## Flow

```
Upload PDF/TXT/MD or paste HTTPS URL
  → Text extraction (PdfPig / UTF-8 / HTML strip)
  → Stored on IntakeCopilotSession (PolicySourceLabel, PolicySourceText)
  → Auto-trigger analyze turn
  → Draft with Compliance template bias
  → User reviews form → Submit → Existing analysis pipeline
```

## Supported inputs

| Source | Formats | Limits |
|--------|---------|--------|
| File upload | `.pdf`, `.txt`, `.md`, `.csv` | 10 MB request size; 50k chars stored |
| URL | HTTPS (optional allowlist) | 2 MB download; 50k chars stored |

## Security

- URL fetch blocks localhost and private IP ranges (SSRF mitigation)
- Optional `PolicyIntake:AllowedHosts` configuration restricts fetch targets
- 15s HTTP timeout via named `PolicyUrlFetcher` client
- Intake disclaimer: assistance only, not legal advice

## API

| Method | Route | Body |
|--------|-------|------|
| POST | `/web-api/spa/intake/sessions/{id}/policy-document` | `multipart/form-data` field `file` |
| POST | `/web-api/spa/intake/sessions/{id}/policy-url` | `{ "url": "https://..." }` |

Both endpoints return the same `IntakeCopilotTurnResponse` as a message turn.

## Submit & provenance

When the user started via Intake Copilot, **Submit Request** on Create Request calls:

`POST /web-api/spa/intake/sessions/{id}/create-request`

with optional form overrides (title, description, template, etc.). The handler:

- Links `IntakeCopilotSession.CreatedRequestId` to the new request
- Appends `Policy source: {label}` to supporting notes when policy was attached
- Applies user-edited form values over the stored draft

Policy text stored on the session is PII-redacted before persistence.

## Configuration

```json
{
  "PolicyIntake": {
    "AllowedHosts": ["example.com", "trust.example.gov"]
  }
}
```

When `AllowedHosts` is empty, any public HTTPS host is permitted (still subject to SSRF blocks).

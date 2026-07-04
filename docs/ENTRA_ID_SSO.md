# Azure Entra ID (Microsoft SSO) Setup

EnhancementHub supports OpenID Connect sign-in for the Razor Pages Web app. Entra ID is the recommended identity provider for enterprise pilots.

## Enable SSO

Set `Authentication:OpenIdConnect:Enabled` to `true` in `src/EnhancementHub.Web/appsettings.Production.json` (or environment variables).

```json
{
  "Authentication": {
    "OpenIdConnect": {
      "Enabled": true,
      "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
      "ClientId": "{app-registration-client-id}",
      "ClientSecret": "{client-secret}",
      "CallbackPath": "/signin-oidc",
      "SignedOutCallbackPath": "/signout-callback-oidc",
      "DefaultRole": "Developer",
      "Scopes": [ "openid", "profile", "email" ],
      "RoleMappings": {
        "{entra-group-or-app-role-id}": "Admin",
        "{another-group-id}": "Approver"
      }
    }
  }
}
```

Production startup validates Authority, ClientId, ClientSecret, and requires either `DefaultRole` or at least one `RoleMappings` entry.

## Entra app registration

1. In Azure Portal → **Microsoft Entra ID** → **App registrations** → **New registration**.
2. Name: `EnhancementHub Web`.
3. Supported account types: single tenant (or multi-tenant if required).
4. Redirect URI: **Web** → `https://{your-host}/signin-oidc`.
5. Create a **client secret** and store it in your secrets manager (not in source control).
6. Under **Authentication**, enable **ID tokens** and add the sign-out redirect URI if using federated logout.

## Group and role mapping

EnhancementHub maps Entra claims to application roles (`Admin`, `Approver`, `Developer`, etc.) using `RoleMappings`:

| Claim source | When to use |
|--------------|-------------|
| **Security group object IDs** (`groups` claim) | Map AD groups to EnhancementHub roles |
| **App roles** (`roles` claim) | Map roles defined on the app registration |

To emit group claims for security groups:

1. App registration → **Token configuration** → **Add groups claim**.
2. Choose **Security groups** (or **Groups assigned to the application** for large tenants).
3. Use each group's **Object ID** as the key in `RoleMappings`.

For app roles, define roles under **App roles** in the registration and assign users/groups under **Enterprise applications → Users and groups**. Map the role value (e.g. `EnhancementHub.Admin`) in `RoleMappings`.

If no mapped role matches, `DefaultRole` is applied (typically `Developer`).

## Claims used by EnhancementHub

| Claim | Purpose |
|-------|---------|
| `oid` / `sub` | Stable user identifier (`NameIdentifier`) |
| `preferred_username` / `email` | Login name and email |
| `name` | Display name |
| `groups` | Security group membership → role mapping |
| `roles` | App roles → role mapping |

PKCE is enabled for the authorization code flow.

## API authentication

The REST API continues to use JWT bearer tokens issued by EnhancementHub (`POST /api/auth/login`) or your own identity integration. Entra SSO applies to the Web UI cookie session. For API-only clients, configure a separate Entra app registration with appropriate scopes if needed.

## Troubleshooting

| Symptom | Check |
|---------|--------|
| Login redirects but user has no permissions | `RoleMappings` keys match group **object IDs** or app role names exactly |
| `groups` claim missing | Token configuration on app registration; group overage may require Microsoft Graph |
| Startup fails in Production | All required OIDC settings and `DataProtection:KeysPath` configured |
| Works locally, fails in prod | Redirect URI matches deployed URL; HTTPS enabled |

## Related configuration

- `DataProtection:KeysPath` — required in Production so auth cookies and encrypted secrets persist across restarts (`docs/DEPLOYMENT.md`).
- `Jwt:Secret` — still required for API tokens even when Web SSO is enabled.

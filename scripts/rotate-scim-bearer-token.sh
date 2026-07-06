#!/usr/bin/env bash
# Rotate the SCIM bearer token used by Entra ID provisioning (Scim:BearerToken).
# Run from repo root. Requires openssl.
set -euo pipefail

NEW_TOKEN="$(openssl rand -base64 48 | tr -d '/+=' | head -c 64)"
TIMESTAMP="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

echo "Generated SCIM bearer token (store securely; shown once):"
echo "$NEW_TOKEN"
echo
echo "Kubernetes / Helm (external secret or values):"
echo "  Scim__BearerToken=$NEW_TOKEN"
echo
echo "Docker Compose / environment file:"
echo "  Scim__BearerToken=$NEW_TOKEN"
echo
echo "Azure Key Vault secret name suggestion: scim-bearer-token"
echo "Rotation recorded at: $TIMESTAMP"
echo
echo "After updating config:"
echo "  1. Rolling restart API pods/instances"
echo "  2. Update Entra enterprise app provisioning credentials with the new token"
echo "  3. Run a SCIM test user provision/deprovision in a sandbox group"
echo "  4. Revoke the previous token in your secrets manager version history"

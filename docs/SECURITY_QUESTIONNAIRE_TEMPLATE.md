# Security Questionnaire — Design Partner Response Template

**Partner:** _Helix (regional healthcare ISV)_  
**Status:** Draft for CS + security review  
**Related:** [SECURITY.md](SECURITY.md) · [SOC2_READINESS.md](SOC2_READINESS.md) · [DESIGN_PARTNER_2_TRACKER.md](DESIGN_PARTNER_2_TRACKER.md)

Use this template to respond to customer InfoSec questionnaires. Copy sections into the partner's form; attach `SECURITY.md` and SOC 2 control mapping as supporting evidence.

---

## 1. Deployment & data residency

| Question | Response |
|----------|----------|
| Where is data stored? | Customer-hosted deployment; all application data in customer-controlled PostgreSQL and object storage. |
| Multi-tenant isolation? | Schema-per-tenant isolation with search_path enforcement; tenant context on every request. |
| Data residency options? | Customer selects region at signup (US, EU, APAC metadata); infrastructure runs in customer cloud. |
| Subprocessors? | Optional OpenAI/Azure OpenAI when customer enables AI (customer API keys). No mandatory SaaS data processor for core workflow. |

---

## 2. Authentication & access control

| Question | Response |
|----------|----------|
| SSO supported? | Entra ID / OIDC (SAML via IdP configuration). |
| SCIM provisioning? | SCIM 2.0 user provisioning (`POST /scim/v2/Users`); bearer token rotation documented in `SECURITY.md`. |
| RBAC model? | Roles: Admin, Architect, Approver, Contributor, Viewer; resource scoping by team membership. |
| Session management? | HTTP-only cookies; configurable session timeout; forced re-auth for sensitive admin actions. |

---

## 3. Encryption

| Question | Response |
|----------|----------|
| Data in transit? | TLS 1.2+ required in production; HSTS on Web host. |
| Data at rest? | Customer-managed database and storage encryption (Azure/AWS disk encryption). |
| Secrets management? | Documented Key Vault / Secrets Manager integration in `DEPLOYMENT.md`. |

---

## 4. Logging, audit, retention

| Question | Response |
|----------|----------|
| Audit log? | Immutable audit events for auth, admin, and workflow actions; export API with signed download tokens. |
| Retention? | Configurable retention policies (`Retention:Enabled`, per-category day counts). |
| Observability? | OpenTelemetry metrics/traces; enabled by default in Helm and `docker-compose.prod.yml`. |

---

## 5. Vulnerability management

| Question | Response |
|----------|----------|
| Dependency scanning? | CI fails on Critical NuGet vulnerabilities; CodeQL on push/PR. |
| Penetration testing? | Third-party pen test scheduled (Wave D); remediated critical/high before customer share. |
| SOC 2? | Type II audit kickoff in progress; control mapping in `SOC2_READINESS.md`. |

---

## 6. Business continuity

| Question | Response |
|----------|----------|
| HA reference? | Helm `values-ha.yaml` — 2+ API/Web/Worker replicas; external Postgres HA. |
| Backup? | Customer-operated database backups; documented in deployment guide. |

---

## Sign-off

| Role | Name | Date |
|------|------|------|
| Security engineer | _Pending_ | |
| Customer success | _Pending_ | |
| Partner security contact | _Pending_ | |

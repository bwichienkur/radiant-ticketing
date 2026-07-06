import { useCallback, useEffect, useState } from 'react';
import {
  createServiceApiKey,
  listAdminTeams,
  listServiceApiKeys,
  revokeServiceApiKey,
} from '../../api/spaClient';
import {
  AlertBanner,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  useToast,
} from '../../components/ui';
import type { CreateServiceApiKeyResult, ServiceApiKeySummary, TeamSummary } from '../../types/spa';

const USER_ROLES = ['Admin', 'Submitter', 'Reviewer', 'Approver', 'Developer'] as const;

export function SettingsApiKeysSection() {
  const toast = useToast();
  const [apiKeys, setApiKeys] = useState<ServiceApiKeySummary[]>([]);
  const [teams, setTeams] = useState<TeamSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [revokingId, setRevokingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [createdKey, setCreatedKey] = useState<CreateServiceApiKeyResult | null>(null);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [role, setRole] = useState<(typeof USER_ROLES)[number]>('Developer');
  const [teamId, setTeamId] = useState('');
  const [expiresInDays, setExpiresInDays] = useState('');

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [keys, teamList] = await Promise.all([listServiceApiKeys(), listAdminTeams()]);
      setApiKeys(keys);
      setTeams(teamList);
    } catch {
      setError('Failed to load API keys.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function handleCreate(event: React.FormEvent) {
    event.preventDefault();
    if (!name.trim()) {
      return;
    }

    setSubmitting(true);
    try {
      const result = await createServiceApiKey({
        name: name.trim(),
        description: description.trim() || undefined,
        role,
        teamId: teamId || undefined,
        expiresInDays: expiresInDays ? Number(expiresInDays) : undefined,
      });
      setCreatedKey(result);
      toast.success('API key created', result.name);
      setName('');
      setDescription('');
      setExpiresInDays('');
      await reload();
    } catch (err) {
      toast.danger('Create failed', err instanceof Error ? err.message : 'Could not create API key.');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleRevoke(keyId: string) {
    if (!window.confirm('Revoke this API key? Integrations using it will stop working.')) {
      return;
    }

    setRevokingId(keyId);
    try {
      await revokeServiceApiKey(keyId);
      toast.success('API key revoked');
      await reload();
    } catch {
      toast.danger('Revoke failed', 'Could not revoke API key.');
    } finally {
      setRevokingId(null);
    }
  }

  if (loading) {
    return <LoadingState label="Loading API keys…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void reload()} />;
  }

  return (
    <div>
      <PageHeader
        title="Service API keys"
        description="Machine-to-machine authentication for integrations and automation"
      />

      {createdKey ? (
        <AlertBanner variant="warning" title="Copy this API key now — it will not be shown again." className="mb-3">
          <pre className="mb-0 mt-2 user-select-all">{createdKey.apiKey}</pre>
          <p className="small mb-0 mt-2">
            Send requests with header <code>X-Api-Key: &lt;key&gt;</code>
          </p>
        </AlertBanner>
      ) : null}

      <div className="row g-4">
        <div className="col-lg-4">
          <form className="card-panel p-4" onSubmit={(event) => void handleCreate(event)}>
            <h2 className="h6 mb-3">Create API key</h2>
            <FormField label="Name" id="api-key-name" required>
              <input
                id="api-key-name"
                className="form-control"
                value={name}
                onChange={(event) => setName(event.target.value)}
                required
                maxLength={200}
                placeholder="CI pipeline"
              />
            </FormField>
            <FormField label="Description" id="api-key-description">
              <textarea
                id="api-key-description"
                className="form-control"
                rows={2}
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                maxLength={1000}
              />
            </FormField>
            <FormField label="Role" id="api-key-role">
              <select
                id="api-key-role"
                className="form-select"
                value={role}
                onChange={(event) => setRole(event.target.value as (typeof USER_ROLES)[number])}
              >
                {USER_ROLES.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Team (optional)" id="api-key-team">
              <select
                id="api-key-team"
                className="form-select"
                value={teamId}
                onChange={(event) => setTeamId(event.target.value)}
              >
                <option value="">— No team scope —</option>
                {teams.map((team) => (
                  <option key={team.id} value={team.id}>
                    {team.name}
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Expires in days (optional)" id="api-key-expires">
              <input
                id="api-key-expires"
                type="number"
                min={1}
                className="form-control"
                value={expiresInDays}
                onChange={(event) => setExpiresInDays(event.target.value)}
              />
            </FormField>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? 'Generating…' : 'Generate key'}
            </button>
          </form>
        </div>

        <div className="col-lg-8">
          <div className="card-panel">
            <div className="card-header eh-section-title px-3 py-3">Active keys</div>
            {apiKeys.length === 0 ? (
              <div className="p-4 text-muted">No API keys yet.</div>
            ) : (
              <div className="table-responsive">
                <table className="table table-enterprise mb-0">
                  <thead>
                    <tr>
                      <th scope="col">Name</th>
                      <th scope="col">Prefix</th>
                      <th scope="col">Role</th>
                      <th scope="col">Status</th>
                      <th scope="col">Last used</th>
                      <th scope="col" />
                    </tr>
                  </thead>
                  <tbody>
                    {apiKeys.map((key) => {
                      const active =
                        key.isActive &&
                        (!key.expiresAt || new Date(key.expiresAt) > new Date());
                      return (
                        <tr key={key.id}>
                          <td>
                            <strong>{key.name}</strong>
                            {key.description ? (
                              <div className="small text-muted">{key.description}</div>
                            ) : null}
                          </td>
                          <td>
                            <code>{key.keyPrefix}…</code>
                          </td>
                          <td>{key.role}</td>
                          <td>
                            <span className={`badge ${active ? 'text-bg-success' : 'text-bg-secondary'}`}>
                              {active ? 'Active' : 'Inactive'}
                            </span>
                          </td>
                          <td className="small">
                            {key.lastUsedAt ? new Date(key.lastUsedAt).toLocaleString() : 'Never'}
                          </td>
                          <td className="text-end">
                            {key.isActive ? (
                              <button
                                type="button"
                                className="btn btn-sm btn-outline-danger"
                                disabled={revokingId === key.id}
                                onClick={() => void handleRevoke(key.id)}
                              >
                                Revoke
                              </button>
                            ) : null}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

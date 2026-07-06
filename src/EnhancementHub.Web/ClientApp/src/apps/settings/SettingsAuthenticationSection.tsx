import { useCallback, useEffect, useState } from 'react';
import { getAuthenticationConfigurationStatus } from '../../api/spaClient';
import { ErrorState, LoadingState, PageHeader } from '../../components/ui';
import type { AuthenticationConfigurationStatus } from '../../types/spa';

export function SettingsAuthenticationSection() {
  const [status, setStatus] = useState<AuthenticationConfigurationStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setStatus(await getAuthenticationConfigurationStatus());
    } catch {
      setError('Failed to load authentication configuration.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  if (loading) {
    return <LoadingState label="Loading authentication status…" />;
  }

  if (error || !status) {
    return <ErrorState message={error ?? 'Unable to load authentication configuration.'} onRetry={() => void reload()} />;
  }

  return (
    <div>
      <PageHeader
        title="Authentication"
        description="OpenID Connect / Entra ID configuration and role mapping validation"
        actions={
          status.openIdConnectEnabled ? (
            <span
              className={`badge ${status.isProductionReady ? 'text-bg-success' : 'text-bg-warning'} badge-status`}
            >
              {status.isProductionReady ? 'Production ready' : 'Needs attention'}
            </span>
          ) : (
            <span className="badge text-bg-secondary badge-status">Local auth</span>
          )
        }
      />

      <div className="row g-4 mb-4">
        <div className="col-md-6">
          <div className="card-panel p-4 h-100">
            <h2 className="h5 mb-3">OpenID Connect</h2>
            <dl className="row mb-0 small">
              <dt className="col-sm-4">Enabled</dt>
              <dd className="col-sm-8">{status.openIdConnectEnabled ? 'Yes' : 'No'}</dd>
              <dt className="col-sm-4">Authority</dt>
              <dd className="col-sm-8">{status.authority ?? '—'}</dd>
              <dt className="col-sm-4">Client ID</dt>
              <dd className="col-sm-8">{status.clientId ?? '—'}</dd>
              <dt className="col-sm-4">Client secret</dt>
              <dd className="col-sm-8">{status.clientSecretConfigured ? 'Configured' : 'Missing'}</dd>
              <dt className="col-sm-4">Default role</dt>
              <dd className="col-sm-8">{status.defaultRole ?? '—'}</dd>
              <dt className="col-sm-4">Scopes</dt>
              <dd className="col-sm-8">{status.scopes.join(', ')}</dd>
            </dl>
          </div>
        </div>
        <div className="col-md-6">
          <div className="card-panel p-4 h-100">
            <h2 className="h5 mb-3">Validation</h2>
            {status.issues.length === 0 ? (
              <p className="text-success mb-0">No configuration issues detected.</p>
            ) : (
              <ul className="list-group list-group-flush">
                {status.issues.map((issue) => (
                  <li
                    key={`${issue.severity}-${issue.message}`}
                    className={`list-group-item px-0 ${
                      issue.severity === 'Error'
                        ? 'list-group-item-danger'
                        : issue.severity === 'Warning'
                          ? 'list-group-item-warning'
                          : 'list-group-item-light'
                    }`}
                  >
                    <strong>{issue.severity}:</strong> {issue.message}
                  </li>
                ))}
              </ul>
            )}
            <p className="small text-muted mt-3 mb-0">
              See <code>docs/ENTRA_ID_SSO.md</code> for Entra app registration steps.
            </p>
          </div>
        </div>
      </div>

      <div className="card-panel">
        <div className="card-header eh-section-title px-3 py-3">Role mappings</div>
        {status.roleMappings.length === 0 ? (
          <p className="text-muted mb-0 p-3">
            No role mappings configured. Users receive the default role when SSO is enabled.
          </p>
        ) : (
          <div className="table-responsive">
            <table className="table table-enterprise mb-0">
              <thead>
                <tr>
                  <th scope="col">Entra group / role ID</th>
                  <th scope="col">Application role</th>
                  <th scope="col">Valid role</th>
                  <th scope="col">GUID format</th>
                </tr>
              </thead>
              <tbody>
                {status.roleMappings.map((mapping) => (
                  <tr key={mapping.source}>
                    <td>
                      <code>{mapping.source}</code>
                    </td>
                    <td>{mapping.targetRole}</td>
                    <td>{mapping.isValidTargetRole ? '✓' : '✗'}</td>
                    <td>{mapping.isGuidFormat ? '✓' : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

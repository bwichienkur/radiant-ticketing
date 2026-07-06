import { useCallback, useEffect, useState } from 'react';
import {
  listDatabaseConnections,
  triggerDatabaseScan,
} from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { EmptyState, ErrorState, LoadingState, PageHeader, useToast } from '../components/ui';
import type { DatabaseConnectionSummary } from '../types/spa';

export function DatabaseConnectionsApp() {
  const toast = useToast();
  const [connections, setConnections] = useState<DatabaseConnectionSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [scanningId, setScanningId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setConnections(await listDatabaseConnections());
    } catch {
      setError('Failed to load database connections.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function handleScan(connectionId: string) {
    setScanningId(connectionId);
    try {
      await triggerDatabaseScan(connectionId);
      toast.success('Scan started', 'Database schema scan is in progress.');
      await reload();
    } catch {
      toast.danger('Scan failed', 'Could not start database scan.');
    } finally {
      setScanningId(null);
    }
  }

  if (loading) {
    return <LoadingState label="Loading database connections…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void reload()} />;
  }

  return (
    <div aria-live="polite">
      <PageHeader
        title="Database connections"
        description="Read-only schema scanning for architecture documentation"
        actions={
          <>
            <SpaLink href="/Spa/OnboardingWizard" className="btn btn-outline-primary">
              Add via setup wizard
            </SpaLink>
            <SpaLink href="/Spa/DatabaseConnections/Register" className="btn btn-primary">
              Register connection
            </SpaLink>
          </>
        }
      />

      {connections.length === 0 ? (
        <EmptyState
          title="No database connections"
          description="Register a read-only database during onboarding, or use the on-prem agent for air-gapped environments."
          icon="inbox"
          action={
            <SpaLink href="/Spa/OnboardingWizard" className="btn btn-primary">
              Start setup wizard
            </SpaLink>
          }
        />
      ) : (
        <div className="card-panel table-desktop-only">
          <div className="table-responsive">
            <table className="table table-hover table-enterprise mb-0">
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col">Application</th>
                  <th scope="col">Provider</th>
                  <th scope="col">Status</th>
                  <th scope="col">Last scan</th>
                  <th scope="col" />
                </tr>
              </thead>
              <tbody>
                {connections.map((connection) => (
                  <tr key={connection.id}>
                    <td>
                      <strong>{connection.name}</strong>
                    </td>
                    <td>{connection.applicationName ?? '—'}</td>
                    <td>{connection.provider ?? '—'}</td>
                    <td>
                      <span
                        className={`badge text-bg-${
                          connection.scanStatus === 'Completed' ? 'success' : 'secondary'
                        } badge-status`}
                      >
                        {connection.scanStatus}
                      </span>
                    </td>
                    <td>
                      {connection.lastScannedAt
                        ? new Date(connection.lastScannedAt).toLocaleString()
                        : '—'}
                    </td>
                    <td className="text-end">
                      <SpaLink
                        href={`/Spa/DatabaseConnections/${connection.id}`}
                        className="btn btn-sm btn-outline-primary me-1"
                      >
                        Details
                      </SpaLink>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary"
                        disabled={scanningId === connection.id}
                        onClick={() => void handleScan(connection.id)}
                      >
                        {scanningId === connection.id ? 'Scanning…' : 'Scan'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

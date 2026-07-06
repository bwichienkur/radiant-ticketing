import { useCallback, useEffect, useState } from 'react';
import {
  listDatabaseConnections,
  triggerDatabaseScan,
} from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { EmptyState, ErrorState, LoadingState, PageHeader, ResponsiveDataList, useToast } from '../components/ui';
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
        <ResponsiveDataList
          items={connections}
          getRowKey={(connection) => connection.id}
          columns={[
            {
              id: 'name',
              header: 'Name',
              cell: (connection) => <strong>{connection.name}</strong>,
            },
            {
              id: 'application',
              header: 'Application',
              cell: (connection) => connection.applicationName ?? '—',
            },
            {
              id: 'provider',
              header: 'Provider',
              cell: (connection) => connection.provider ?? '—',
            },
            {
              id: 'status',
              header: 'Status',
              cell: (connection) => (
                <span
                  className={`badge text-bg-${
                    connection.scanStatus === 'Completed' ? 'success' : 'secondary'
                  } badge-status`}
                >
                  {connection.scanStatus}
                </span>
              ),
            },
            {
              id: 'scanned',
              header: 'Last scan',
              cell: (connection) =>
                connection.lastScannedAt ? new Date(connection.lastScannedAt).toLocaleString() : '—',
            },
            {
              id: 'actions',
              header: '',
              cell: (connection) => (
                <>
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
                </>
              ),
              cellClassName: 'text-end',
            },
          ]}
          renderMobileCard={(connection) => (
            <>
              <div className="mobile-data-card-title">{connection.name}</div>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">Application</span>
                <span>{connection.applicationName ?? '—'}</span>
              </div>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">Status</span>
                <span>{connection.scanStatus}</span>
              </div>
              <div className="mobile-data-card-actions">
                <SpaLink
                  href={`/Spa/DatabaseConnections/${connection.id}`}
                  className="btn btn-sm btn-outline-primary"
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
              </div>
            </>
          )}
        />
      )}
    </div>
  );
}

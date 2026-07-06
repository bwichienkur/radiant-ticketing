import { FormEvent, useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  detectSchemaDrift,
  getDriftReport,
  listDriftConnections,
} from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import {
  AlertBanner,
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
  useToast,
} from '../components/ui';
import type { DatabaseConnectionSummary, DriftReport } from '../types/spa';

function severityBadgeClass(severity: string): string {
  switch (severity) {
    case 'Critical':
      return 'text-bg-danger';
    case 'High':
      return 'text-bg-warning';
    case 'Medium':
      return 'text-bg-info';
    default:
      return 'text-bg-secondary';
  }
}

export function SchemaDriftApp() {
  const toast = useToast();
  const [searchParams, setSearchParams] = useSearchParams();
  const [connections, setConnections] = useState<DatabaseConnectionSummary[]>([]);
  const [report, setReport] = useState<DriftReport | null>(null);
  const [connectionId, setConnectionId] = useState(searchParams.get('connectionId') ?? '');
  const [loading, setLoading] = useState(true);
  const [detecting, setDetecting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadReport = useCallback(async (activeConnectionId: string) => {
    if (!activeConnectionId) {
      setReport(null);
      return;
    }

    setReport(await getDriftReport(activeConnectionId));
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);
      try {
        const items = await listDriftConnections();
        if (cancelled) {
          return;
        }

        setConnections(items);
        const initialId =
          searchParams.get('connectionId') ?? items[0]?.id ?? '';
        setConnectionId(initialId);
        if (initialId) {
          await loadReport(initialId);
        }
      } catch {
        if (!cancelled) {
          setError('Failed to load drift data.');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, [loadReport, searchParams]);

  async function handleViewReport(event: FormEvent) {
    event.preventDefault();
    if (!connectionId) {
      return;
    }

    setSearchParams(connectionId ? { connectionId } : {});
    setLoading(true);
    try {
      await loadReport(connectionId);
    } catch {
      setError('Failed to load drift report.');
    } finally {
      setLoading(false);
    }
  }

  async function handleDetect() {
    if (!connectionId) {
      return;
    }

    setDetecting(true);
    setError(null);
    try {
      const result = await detectSchemaDrift(connectionId);
      setReport(result);
      toast.success('Detection complete', `${result.findings.length} finding(s) recorded.`);
    } catch {
      toast.danger('Detection failed', 'Could not run schema drift detection.');
    } finally {
      setDetecting(false);
    }
  }

  if (loading && connections.length === 0 && !error) {
    return <LoadingState label="Loading schema drift…" />;
  }

  if (error && connections.length === 0) {
    return <ErrorState message={error} onRetry={() => window.location.reload()} />;
  }

  return (
    <div aria-live="polite">
      <PageHeader
        title="Schema drift detection"
        description="Compare live database schema against indexed code expectations"
      />

      {connections.length === 0 ? (
        <EmptyState
          title="No databases connected"
          description="Connect a database via onboarding to run drift detection."
          icon="inbox"
          action={
            <SpaLink href="/Spa/OnboardingWizard" className="btn btn-primary">
              Start setup wizard
            </SpaLink>
          }
        />
      ) : (
        <>
          <form
            className="card-panel p-3 mb-4 eh-filter-panel"
            onSubmit={(e) => void handleViewReport(e)}
            aria-label="Drift detection filters"
          >
            <div className="row g-3 align-items-end">
              <div className="col-md-5">
                <label className="form-label" htmlFor="drift-connection">
                  Database connection
                </label>
                <select
                  id="drift-connection"
                  className="form-select"
                  value={connectionId}
                  onChange={(event) => setConnectionId(event.target.value)}
                  required
                >
                  {connections.map((connection) => (
                    <option key={connection.id} value={connection.id}>
                      {connection.name} ({connection.applicationName ?? 'Application'})
                    </option>
                  ))}
                </select>
              </div>
              <div className="col-auto d-flex flex-wrap gap-2">
                <button type="submit" className="btn btn-primary" disabled={loading}>
                  View report
                </button>
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  disabled={detecting || !connectionId}
                  onClick={() => void handleDetect()}
                >
                  {detecting ? 'Detecting…' : 'Run detection'}
                </button>
              </div>
            </div>
          </form>

          {loading ? <LoadingState label="Loading drift report…" /> : null}

          {!loading && report ? (
            <>
              <AlertBanner variant="info" className="mb-3">
                {report.findings.length} finding(s) detected
                {report.detectedAt
                  ? ` · last scan ${new Date(report.detectedAt).toLocaleString()}`
                  : ''}
              </AlertBanner>
              {report.findings.length === 0 ? (
                <EmptyState
                  title="No drift findings"
                  description="Schema matches indexed code expectations for this connection."
                  icon="search"
                />
              ) : (
                report.findings.map((finding) => (
                  <div
                    key={finding.id}
                    className="card-panel mb-2 p-3 border-start border-4 border-warning"
                  >
                    <div className="d-flex justify-content-between flex-wrap gap-2">
                      <strong>{finding.title}</strong>
                      <span className={`badge ${severityBadgeClass(finding.severity)} badge-status`}>
                        {finding.severity}
                      </span>
                    </div>
                    <p className="small text-muted mb-0 mt-1">{finding.description}</p>
                  </div>
                ))
              )}
            </>
          ) : null}
        </>
      )}
    </div>
  );
}

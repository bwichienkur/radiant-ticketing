import { useCallback, useEffect, useState } from 'react';
import { getAdminCompliance } from '../../api/spaClient';
import { AlertBanner, ErrorState, LoadingState, SectionCard } from '../../components/ui';
import type { AdminComplianceResponse } from '../../types/spa';

export function AdminComplianceSection() {
  const [data, setData] = useState<AdminComplianceResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setData(await getAdminCompliance());
    } catch {
      setError('Failed to load SOC 2 readiness report.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  if (loading) {
    return <LoadingState label="Loading compliance report…" />;
  }

  if (error || !data) {
    return <ErrorState message={error ?? 'Unable to load report.'} onRetry={() => void reload()} />;
  }

  const { report, runtimeStatus } = data;

  return (
    <div aria-live="polite">
      <h2 className="eh-section-title mb-1">SOC 2 readiness</h2>
      <p className="text-muted small mb-3">
        Control-to-feature mapping for security questionnaires — not a SOC 2 attestation.
      </p>

      <div className="d-flex flex-wrap gap-2 mb-3">
        <span className="badge text-bg-success">{report.implementedCount} implemented</span>
        <span className="badge text-bg-warning">{report.partialCount} partial</span>
        {report.gapCount > 0 ? (
          <span className="badge text-bg-danger">{report.gapCount} gaps</span>
        ) : null}
      </div>

      {runtimeStatus.usesSimulatedBackends ? (
        <AlertBanner variant="warning" title="Simulated or offline backends detected." className="mb-3">
          AI: {runtimeStatus.aiProvider} · Vector: {runtimeStatus.vectorSearchProvider} · QA:{' '}
          {runtimeStatus.qaRunner}. Configure production providers before buyer demos.
        </AlertBanner>
      ) : (
        <AlertBanner variant="success" className="mb-3">
          Production backends active — AI: {runtimeStatus.aiProvider} · Vector:{' '}
          {runtimeStatus.vectorSearchProvider} · QA: {runtimeStatus.qaRunner}.
        </AlertBanner>
      )}

      <SectionCard title="Control mapping">
        <div className="table-responsive">
          <table className="table table-enterprise mb-0">
            <thead>
              <tr>
                <th scope="col">Control</th>
                <th scope="col">Category</th>
                <th scope="col">Control area</th>
                <th scope="col">Feature</th>
                <th scope="col">Status</th>
                <th scope="col">Configuration</th>
              </tr>
            </thead>
            <tbody>
              {report.controls.map((control) => (
                <tr key={control.controlId}>
                  <td>
                    <code>{control.controlId}</code>
                  </td>
                  <td className="small">{control.trustServiceCategory}</td>
                  <td>{control.title}</td>
                  <td className="small">{control.enhancementHubFeature}</td>
                  <td>
                    <span className={`badge badge-status ${statusBadgeClass(control.status)}`}>
                      {control.status}
                    </span>
                  </td>
                  <td className="small text-muted">{control.configurationHint ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </SectionCard>
    </div>
  );
}

function statusBadgeClass(status: string): string {
  if (status === 'Implemented') return 'text-bg-success';
  if (status === 'Partial') return 'text-bg-warning';
  return 'text-bg-danger';
}

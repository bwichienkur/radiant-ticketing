import { useCallback, useEffect, useState } from 'react';
import { getAdminObservability } from '../../api/spaClient';
import { ErrorState, LoadingState, SectionCard } from '../../components/ui';
import type { ObservabilityStatus } from '../../types/spa';

export function AdminObservabilitySection() {
  const [data, setData] = useState<ObservabilityStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setData(await getAdminObservability());
    } catch {
      setError('Failed to load observability status.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  if (loading) {
    return <LoadingState label="Loading observability…" />;
  }

  if (error || !data) {
    return <ErrorState message={error ?? 'Unable to load status.'} onRetry={() => void reload()} />;
  }

  return (
    <div aria-live="polite">
      <h2 className="eh-section-title mb-1">Observability</h2>
      <p className="text-muted small mb-3">OpenTelemetry, data protection, and HA readiness</p>

      <SectionCard title="OpenTelemetry" className="mb-4">
        <dl className="row mb-0">
          <dt className="col-sm-4">Enabled</dt>
          <dd className="col-sm-8">{data.openTelemetry.enabled ? 'Yes' : 'No'}</dd>
          <dt className="col-sm-4">Service</dt>
          <dd className="col-sm-8">{data.openTelemetry.serviceName}</dd>
          <dt className="col-sm-4">OTLP endpoint</dt>
          <dd className="col-sm-8">{data.openTelemetry.otlpEndpoint ?? '—'}</dd>
          <dt className="col-sm-4">Instrumentations</dt>
          <dd className="col-sm-8">{data.openTelemetry.activeInstrumentations.join(', ') || '—'}</dd>
        </dl>
      </SectionCard>

      <SectionCard title="Data protection" className="mb-4">
        <dl className="row mb-0">
          <dt className="col-sm-4">Storage</dt>
          <dd className="col-sm-8">{data.dataProtection.storageProvider}</dd>
          <dt className="col-sm-4">Shared key ring</dt>
          <dd className="col-sm-8">{data.dataProtection.sharedKeyRingConfigured ? 'Yes' : 'No'}</dd>
        </dl>
        {data.dataProtection.issues.length > 0 ? (
          <ul className="small text-muted mt-3 mb-0">
            {data.dataProtection.issues.map((issue) => (
              <li key={issue.message}>{issue.severity}: {issue.message}</li>
            ))}
          </ul>
        ) : null}
      </SectionCard>

      <SectionCard title="High availability">
        <ul className="mb-0">
          <li>Postgres: {data.highAvailability.postgresConfigured ? '✓' : '—'}</li>
          <li>Hangfire: {data.highAvailability.hangfireConfigured ? '✓' : '—'}</li>
          <li>Read replica: {data.highAvailability.readReplicaConfigured ? '✓' : '—'}</li>
          <li>Vector offload: {data.highAvailability.vectorOffloadConfigured ? '✓' : '—'}</li>
          <li>Observability: {data.highAvailability.observabilityEnabled ? '✓' : '—'}</li>
        </ul>
        {data.highAvailability.recommendations.length > 0 ? (
          <ul className="small text-muted mt-3 mb-0">
            {data.highAvailability.recommendations.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        ) : null}
      </SectionCard>
    </div>
  );
}

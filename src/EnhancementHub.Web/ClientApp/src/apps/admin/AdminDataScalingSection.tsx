import { useCallback, useEffect, useState } from 'react';
import { getAdminDataScaling } from '../../api/spaClient';
import { ErrorState, LoadingState, SectionCard } from '../../components/ui';
import type { DataScalingStatus } from '../../types/spa';

export function AdminDataScalingSection() {
  const [data, setData] = useState<DataScalingStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setData(await getAdminDataScaling());
    } catch {
      setError('Failed to load data scaling status.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  if (loading) {
    return <LoadingState label="Loading data scaling…" />;
  }

  if (error || !data) {
    return <ErrorState message={error ?? 'Unable to load status.'} onRetry={() => void reload()} />;
  }

  return (
    <div aria-live="polite">
      <h2 className="eh-section-title mb-1">Data scaling</h2>
      <p className="text-muted small mb-3">Vector search, read replicas, pooling, and archival posture</p>

      <SectionCard title="Vector search" className="mb-4">
        <dl className="row mb-0">
          <dt className="col-sm-4">Provider</dt>
          <dd className="col-sm-8">{data.vectorSearch.provider}</dd>
          <dt className="col-sm-4">Production ready</dt>
          <dd className="col-sm-8">{data.vectorSearch.isProductionReady ? 'Yes' : 'No'}</dd>
          <dt className="col-sm-4">Dimensions</dt>
          <dd className="col-sm-8">{data.vectorSearch.dimensions}</dd>
        </dl>
      </SectionCard>

      <SectionCard title="Database connections" className="mb-4">
        <dl className="row mb-0">
          <dt className="col-sm-4">Provider</dt>
          <dd className="col-sm-8">{data.database.databaseProvider}</dd>
          <dt className="col-sm-4">Read replica</dt>
          <dd className="col-sm-8">{data.database.readReplicaConfigured ? 'Configured' : 'Not configured'}</dd>
          <dt className="col-sm-4">Max pool size</dt>
          <dd className="col-sm-8">{data.database.maxPoolSize}</dd>
        </dl>
      </SectionCard>

      <SectionCard title="Archival">
        <dl className="row mb-0">
          <dt className="col-sm-4">Audit logs</dt>
          <dd className="col-sm-8">{data.archival.auditLogCount.toLocaleString()}</dd>
          <dt className="col-sm-4">AI prompt runs</dt>
          <dd className="col-sm-8">{data.archival.aiPromptRunCount.toLocaleString()}</dd>
          <dt className="col-sm-4">Eligible for archive</dt>
          <dd className="col-sm-8">{data.archival.eligibleAiPromptRunArchiveCount.toLocaleString()}</dd>
        </dl>
        {data.archival.recommendations.length > 0 ? (
          <ul className="small text-muted mt-3 mb-0">
            {data.archival.recommendations.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        ) : null}
      </SectionCard>
    </div>
  );
}

import { useCallback, useEffect, useState } from 'react';
import { getConnectionErd } from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { ErrorState, LoadingState, PageHeader } from '../components/ui';
import type { ErdDiagram } from '../types/spa';

interface DatabaseConnectionErdAppProps {
  connectionId: string;
}

export function DatabaseConnectionErdApp({ connectionId }: DatabaseConnectionErdAppProps) {
  const [erd, setErd] = useState<ErdDiagram | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setErd(await getConnectionErd(connectionId));
    } catch {
      setError('Failed to load ERD diagram.');
    } finally {
      setLoading(false);
    }
  }, [connectionId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  if (loading) {
    return <LoadingState label="Loading ERD…" />;
  }

  if (error || !erd) {
    return <ErrorState message={error ?? 'ERD not available.'} onRetry={() => void reload()} />;
  }

  return (
    <div>
      <PageHeader
        title="Entity relationship diagram"
        description="Mermaid ERD generated from indexed schema"
        actions={
          <SpaLink
            href={`/Spa/DatabaseConnections/${connectionId}`}
            className="btn btn-outline-secondary btn-sm"
          >
            Back to schema
          </SpaLink>
        }
      />

      <div className="card-panel p-4">
        <pre className="mb-0 small">{erd.mermaid}</pre>
      </div>
    </div>
  );
}

import { useCallback, useEffect, useState } from 'react';
import { getDatabaseSchema } from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { EmptyState, ErrorState, LoadingState, PageHeader } from '../components/ui';
import type { DatabaseSchema } from '../types/spa';

interface DatabaseConnectionDetailsAppProps {
  connectionId: string;
}

export function DatabaseConnectionDetailsApp({ connectionId }: DatabaseConnectionDetailsAppProps) {
  const [schema, setSchema] = useState<DatabaseSchema | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setSchema(await getDatabaseSchema(connectionId));
    } catch {
      setError('Failed to load database schema.');
    } finally {
      setLoading(false);
    }
  }, [connectionId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  if (loading) {
    return <LoadingState label="Loading schema…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void reload()} />;
  }

  if (!schema || schema.tables.length === 0) {
    return (
      <div>
        <PageHeader
          title={schema?.connectionName ?? 'Database connection'}
          description="Schema details and column metadata"
        />
        <EmptyState
          title="Schema not available"
          description="Run a scan from the database connections list to populate schema details."
          icon="inbox"
          action={
            <SpaLink href="/Spa/DatabaseConnections" className="btn btn-primary">
              Back to connections
            </SpaLink>
          }
        />
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={schema.connectionName}
        description="Schema details and column metadata"
        actions={
          <>
            <SpaLink
              href={`/Spa/DatabaseConnections/${connectionId}/erd`}
              className="btn btn-outline-primary btn-sm"
            >
              View ERD
            </SpaLink>
            <SpaLink
              href={`/Spa/SchemaDrift?connectionId=${connectionId}`}
              className="btn btn-outline-secondary btn-sm"
            >
              Drift report
            </SpaLink>
          </>
        }
      />

      {schema.tables.map((table) => (
        <div key={table.id} className="card-panel mb-3">
          <div className="card-header eh-section-title px-3 py-3">
            {table.schemaName}.{table.tableName}
          </div>
          <div className="table-responsive">
            <table className="table table-sm table-enterprise mb-0">
              <thead>
                <tr>
                  <th scope="col">Column</th>
                  <th scope="col">Type</th>
                  <th scope="col">Nullable</th>
                  <th scope="col">PK</th>
                  <th scope="col">FK</th>
                </tr>
              </thead>
              <tbody>
                {table.columns.map((column) => (
                  <tr key={column.name}>
                    <td>{column.name}</td>
                    <td>{column.dataType}</td>
                    <td>{column.isNullable ? 'Yes' : 'No'}</td>
                    <td>{column.isPrimaryKey ? '✓' : ''}</td>
                    <td>{column.isForeignKey ? '✓' : ''}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ))}
    </div>
  );
}

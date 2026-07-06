import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { listApplications, registerDatabaseConnection, triggerDatabaseScan } from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { ErrorState, FormField, LoadingState, PageHeader, useToast } from '../components/ui';
import type { ApplicationListItem } from '../types/spa';

const PROVIDERS = ['SqlServer', 'PostgreSQL', 'Sqlite'] as const;

export function DatabaseConnectionRegisterApp() {
  const navigate = useNavigate();
  const toast = useToast();
  const [applications, setApplications] = useState<ApplicationListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [applicationId, setApplicationId] = useState('');
  const [name, setName] = useState('');
  const [provider, setProvider] = useState<(typeof PROVIDERS)[number]>('Sqlite');
  const [connectionString, setConnectionString] = useState('Data Source=enhancementhub.db');
  const [isReadOnly, setIsReadOnly] = useState(true);

  useEffect(() => {
    void (async () => {
      try {
        const apps = await listApplications();
        setApplications(apps);
        if (apps.length > 0) {
          setApplicationId(apps[0].id);
        }
      } catch {
        setError('Failed to load applications.');
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!applicationId || !name.trim()) {
      return;
    }

    setSubmitting(true);
    try {
      const connection = await registerDatabaseConnection({
        applicationId,
        name: name.trim(),
        provider,
        connectionString,
        isReadOnly,
      });
      await triggerDatabaseScan(connection.id);
      toast.success('Connection registered', 'Schema scan has started.');
      navigate(`/Spa/DatabaseConnections/${connection.id}`);
    } catch {
      toast.danger('Registration failed', 'Could not register database connection.');
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return <LoadingState label="Loading applications…" />;
  }

  if (error) {
    return <ErrorState message={error} />;
  }

  return (
    <div>
      <PageHeader
        title="Register database connection"
        description="Connect a read-only database for schema intelligence"
        actions={
          <SpaLink href="/Spa/DatabaseConnections" className="btn btn-outline-secondary">
            Back to connections
          </SpaLink>
        }
      />

      <form className="card-panel p-4 col-lg-8" onSubmit={(event) => void handleSubmit(event)}>
        <FormField label="Application" id="applicationId">
          <select
            id="applicationId"
            className="form-select"
            value={applicationId}
            onChange={(event) => setApplicationId(event.target.value)}
            required
          >
            {applications.map((app) => (
              <option key={app.id} value={app.id}>
                {app.name}
              </option>
            ))}
          </select>
        </FormField>

        <FormField label="Connection name" id="name">
          <input
            id="name"
            className="form-control"
            value={name}
            onChange={(event) => setName(event.target.value)}
            required
          />
        </FormField>

        <FormField label="Provider" id="provider">
          <select
            id="provider"
            className="form-select"
            value={provider}
            onChange={(event) => setProvider(event.target.value as (typeof PROVIDERS)[number])}
          >
            {PROVIDERS.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>
        </FormField>

        <FormField label="Connection string" id="connectionString">
          <textarea
            id="connectionString"
            className="form-control"
            rows={3}
            value={connectionString}
            onChange={(event) => setConnectionString(event.target.value)}
            required
          />
        </FormField>

        <div className="form-check mb-4">
          <input
            id="isReadOnly"
            type="checkbox"
            className="form-check-input"
            checked={isReadOnly}
            onChange={(event) => setIsReadOnly(event.target.checked)}
          />
          <label className="form-check-label" htmlFor="isReadOnly">
            Read-only connection
          </label>
        </div>

        <button type="submit" className="btn btn-primary" disabled={submitting}>
          {submitting ? 'Registering…' : 'Register & scan'}
        </button>
      </form>
    </div>
  );
}

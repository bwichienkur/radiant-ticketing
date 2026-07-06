import { useEffect, useState } from 'react';
import { exportDocumentation, listApplications } from '../api/spaClient';
import { ErrorState, FormField, LoadingState, PageHeader } from '../components/ui';
import type { ApplicationListItem, DocumentationExportFormat } from '../types/spa';

const FORMATS: DocumentationExportFormat[] = ['Markdown', 'Mermaid', 'Both'];

export function DocumentationExportApp() {
  const [applications, setApplications] = useState<ApplicationListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [applicationId, setApplicationId] = useState('');
  const [format, setFormat] = useState<DocumentationExportFormat>('Both');

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

  function handleExport(event: React.FormEvent) {
    event.preventDefault();
    if (!applicationId) {
      return;
    }

    exportDocumentation(applicationId, format);
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
        title="Export system documentation"
        description="Generate architecture documentation from indexed application intelligence"
      />

      <form className="card-panel p-4 col-lg-7" onSubmit={handleExport} aria-label="Export documentation">
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

        <FormField label="Format" id="format">
          <select
            id="format"
            className="form-select"
            value={format}
            onChange={(event) => setFormat(event.target.value as DocumentationExportFormat)}
          >
            {FORMATS.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>
        </FormField>

        <button type="submit" className="btn btn-primary" disabled={!applicationId}>
          Export
        </button>
      </form>
    </div>
  );
}

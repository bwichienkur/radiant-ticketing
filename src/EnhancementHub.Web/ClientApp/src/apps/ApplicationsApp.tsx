import { useEffect, useState } from 'react';
import { listApplications } from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { EmptyState, ErrorState, LoadingState, PageHeader } from '../components/ui';
import type { ApplicationListItem } from '../types/spa';

export function ApplicationsApp() {
  const [applications, setApplications] = useState<ApplicationListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void listApplications()
      .then(setApplications)
      .catch(() => setError('Failed to load applications.'))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <LoadingState label="Loading applications…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => window.location.reload()} />;
  }

  return (
    <div aria-live="polite">
      <PageHeader
        title="Applications"
        description="Registered enterprise applications"
        actions={
          <SpaLink href="/Spa/OnboardingWizard" className="btn btn-primary">
            Document new application
          </SpaLink>
        }
      />

      {applications.length === 0 ? (
        <EmptyState
          title="No applications yet"
          description="Use the guided setup wizard to register your first application, connect code and databases, and generate documentation."
          icon="document"
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
                  <th scope="col">Domain</th>
                  <th scope="col">Repositories</th>
                  <th scope="col" />
                </tr>
              </thead>
              <tbody>
                {applications.map((app) => (
                  <tr key={app.id}>
                    <td>
                      <strong>{app.name}</strong>
                    </td>
                    <td>{app.businessDomain ?? '—'}</td>
                    <td>{app.repositoryCount}</td>
                    <td className="text-end">
                      <a href={`/Applications/Details/${app.id}`} className="btn btn-sm btn-outline-primary">
                        Profile
                      </a>
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

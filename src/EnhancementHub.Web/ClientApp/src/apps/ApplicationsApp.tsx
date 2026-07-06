import { useEffect, useState } from 'react';
import { listApplications } from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { EmptyState, ErrorState, LoadingState, PageHeader, ResponsiveDataList } from '../components/ui';
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
        <ResponsiveDataList
          items={applications}
          getRowKey={(app) => app.id}
          columns={[
            {
              id: 'name',
              header: 'Name',
              cell: (app) => <strong>{app.name}</strong>,
            },
            {
              id: 'domain',
              header: 'Domain',
              cell: (app) => app.businessDomain ?? '—',
            },
            {
              id: 'repos',
              header: 'Repositories',
              cell: (app) => app.repositoryCount,
            },
            {
              id: 'actions',
              header: '',
              cell: (app) => (
                <SpaLink href={`/Spa/SystemMap?ApplicationId=${app.id}`} className="btn btn-sm btn-outline-primary">
                  System map
                </SpaLink>
              ),
              cellClassName: 'text-end',
            },
          ]}
          renderMobileCard={(app) => (
            <>
              <div className="mobile-data-card-title">{app.name}</div>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">Domain</span>
                <span>{app.businessDomain ?? '—'}</span>
              </div>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">Repositories</span>
                <span>{app.repositoryCount}</span>
              </div>
              <div className="mobile-data-card-actions">
                <SpaLink href={`/Spa/SystemMap?ApplicationId=${app.id}`} className="btn btn-sm btn-outline-primary">
                  System map
                </SpaLink>
              </div>
            </>
          )}
        />
      )}
    </div>
  );
}

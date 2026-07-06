import { useCallback, useEffect, useState } from 'react';
import { listRepositoriesCatalog, triggerRepositoryReindex } from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { EmptyState, ErrorState, LoadingState, PageHeader, ResponsiveDataList, useToast } from '../components/ui';
import type { RepositoryListItem } from '../types/spa';

export function RepositoriesApp() {
  const toast = useToast();
  const [repositories, setRepositories] = useState<RepositoryListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [reindexingId, setReindexingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setRepositories(await listRepositoriesCatalog());
    } catch {
      setError('Failed to load repositories.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function handleReindex(repositoryId: string) {
    setReindexingId(repositoryId);
    try {
      await triggerRepositoryReindex(repositoryId);
      toast.success('Re-index started', 'Repository indexing is in progress.');
      await reload();
    } catch {
      toast.danger('Re-index failed', 'Could not start repository indexing.');
    } finally {
      setReindexingId(null);
    }
  }

  if (loading) {
    return <LoadingState label="Loading repositories…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void reload()} />;
  }

  return (
    <div aria-live="polite">
      <PageHeader
        title="Repositories"
        description="Source repository indexing status"
        actions={
          <SpaLink href="/Spa/OnboardingWizard" className="btn btn-outline-primary">
            Add via setup wizard
          </SpaLink>
        }
      />

      {repositories.length === 0 ? (
        <EmptyState
          title="No repositories connected"
          description="Connect a local repository clone during application onboarding to index code and build architecture intelligence."
          icon="document"
          action={
            <SpaLink href="/Spa/OnboardingWizard" className="btn btn-primary">
              Start setup wizard
            </SpaLink>
          }
        />
      ) : (
        <ResponsiveDataList
          items={repositories}
          getRowKey={(repo) => repo.id}
          columns={[
            {
              id: 'name',
              header: 'Name',
              cell: (repo) => <strong>{repo.name}</strong>,
            },
            {
              id: 'application',
              header: 'Application',
              cell: (repo) => repo.applicationName ?? '—',
            },
            {
              id: 'url',
              header: 'URL',
              cell: (repo) => (
                <span className="small text-truncate d-inline-block" style={{ maxWidth: '12rem' }}>
                  {repo.url}
                </span>
              ),
            },
            {
              id: 'status',
              header: 'Status',
              cell: (repo) => (
                <span className="badge text-bg-secondary badge-status">{repo.indexingStatus}</span>
              ),
            },
            {
              id: 'indexed',
              header: 'Last indexed',
              cell: (repo) => (repo.lastIndexedAt ? new Date(repo.lastIndexedAt).toLocaleString() : '—'),
            },
            {
              id: 'actions',
              header: '',
              cell: (repo) => (
                <button
                  type="button"
                  className="btn btn-sm btn-outline-primary"
                  disabled={reindexingId === repo.id}
                  onClick={() => void handleReindex(repo.id)}
                >
                  {reindexingId === repo.id ? 'Starting…' : 'Re-index'}
                </button>
              ),
              cellClassName: 'text-end',
            },
          ]}
          renderMobileCard={(repo) => (
            <>
              <div className="mobile-data-card-title">{repo.name}</div>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">Application</span>
                <span>{repo.applicationName ?? '—'}</span>
              </div>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">Status</span>
                <span>{repo.indexingStatus}</span>
              </div>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">Last indexed</span>
                <span>{repo.lastIndexedAt ? new Date(repo.lastIndexedAt).toLocaleString() : '—'}</span>
              </div>
              <div className="mobile-data-card-actions">
                <button
                  type="button"
                  className="btn btn-sm btn-outline-primary"
                  disabled={reindexingId === repo.id}
                  onClick={() => void handleReindex(repo.id)}
                >
                  {reindexingId === repo.id ? 'Starting…' : 'Re-index'}
                </button>
              </div>
            </>
          )}
        />
      )}
    </div>
  );
}

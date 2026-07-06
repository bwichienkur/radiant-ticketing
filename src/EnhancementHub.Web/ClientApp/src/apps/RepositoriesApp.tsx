import { useCallback, useEffect, useState } from 'react';
import { listRepositoriesCatalog, triggerRepositoryReindex } from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { EmptyState, ErrorState, LoadingState, PageHeader, useToast } from '../components/ui';
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
        <div className="card-panel table-desktop-only">
          <div className="table-responsive">
            <table className="table table-hover table-enterprise mb-0">
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col">Application</th>
                  <th scope="col">URL</th>
                  <th scope="col">Status</th>
                  <th scope="col">Last indexed</th>
                  <th scope="col" />
                </tr>
              </thead>
              <tbody>
                {repositories.map((repo) => (
                  <tr key={repo.id}>
                    <td>
                      <strong>{repo.name}</strong>
                    </td>
                    <td>{repo.applicationName ?? '—'}</td>
                    <td className="small text-truncate" style={{ maxWidth: '12rem' }}>
                      {repo.url}
                    </td>
                    <td>
                      <span className="badge text-bg-secondary badge-status">{repo.indexingStatus}</span>
                    </td>
                    <td>{repo.lastIndexedAt ? new Date(repo.lastIndexedAt).toLocaleString() : '—'}</td>
                    <td className="text-end">
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-primary"
                        disabled={reindexingId === repo.id}
                        onClick={() => void handleReindex(repo.id)}
                      >
                        {reindexingId === repo.id ? 'Starting…' : 'Re-index'}
                      </button>
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

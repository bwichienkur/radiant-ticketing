import { useCallback, useEffect, useState } from 'react';
import {
  createAdminTeam,
  listAdminTeams,
} from '../../api/spaClient';
import {
  AlertBanner,
  ErrorState,
  FormField,
  LoadingState,
  SectionCard,
  useToast,
} from '../../components/ui';
import type { TeamSummary } from '../../types/spa';

export function SettingsTeamsSection() {
  const toast = useToast();
  const [teams, setTeams] = useState<TeamSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setTeams(await listAdminTeams());
    } catch {
      setError('Failed to load teams.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function handleCreate(event: React.FormEvent) {
    event.preventDefault();
    if (!name.trim()) {
      return;
    }

    setSubmitting(true);
    try {
      const team = await createAdminTeam(name.trim(), description.trim() || undefined);
      toast.success('Team created', team.name);
      setName('');
      setDescription('');
      await reload();
    } catch (err) {
      toast.danger('Create failed', err instanceof Error ? err.message : 'Could not create team.');
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return <LoadingState label="Loading teams…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void reload()} />;
  }

  return (
    <SectionCard title="Teams">
      <p className="text-muted small mb-3">Manage team membership and application ownership</p>

      <div className="row g-4">
        <div className="col-lg-4">
          <form className="card-panel p-4" onSubmit={(event) => void handleCreate(event)}>
            <h2 className="h6 mb-3">Create team</h2>
            <FormField label="Name" id="team-name" required>
              <input
                id="team-name"
                className="form-control"
                value={name}
                onChange={(event) => setName(event.target.value)}
                required
                maxLength={200}
              />
            </FormField>
            <FormField label="Description" id="team-description">
              <textarea
                id="team-description"
                className="form-control"
                rows={2}
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                maxLength={1000}
              />
            </FormField>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? 'Creating…' : 'Create team'}
            </button>
          </form>
        </div>

        <div className="col-lg-8">
          <div className="card-panel">
            <div className="card-header eh-section-title px-3 py-3">All teams</div>
            {teams.length === 0 ? (
              <div className="p-4 text-muted">
                No teams yet. Create one or complete onboarding to auto-create teams.
              </div>
            ) : (
              <div className="table-responsive">
                <table className="table table-enterprise mb-0">
                  <thead>
                    <tr>
                      <th scope="col">Name</th>
                      <th scope="col">Members</th>
                      <th scope="col">Applications</th>
                      <th scope="col" />
                    </tr>
                  </thead>
                  <tbody>
                    {teams.map((team) => (
                      <tr key={team.id}>
                        <td>
                          <strong>{team.name}</strong>
                          {team.description ? (
                            <div className="small text-muted">{team.description}</div>
                          ) : null}
                        </td>
                        <td>{team.memberCount}</td>
                        <td>{team.applicationCount}</td>
                        <td className="text-end">
                          <a
                            className="btn btn-sm btn-outline-primary"
                            href={`/Admin/TeamDetail/${team.id}`}
                          >
                            Manage
                          </a>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>

      <AlertBanner variant="neutral" className="mt-3">
        Team detail management remains on the admin page while member workflows are migrated.
      </AlertBanner>
    </SectionCard>
  );
}

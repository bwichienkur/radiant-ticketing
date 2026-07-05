import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import {
  advanceOnboardingToReview,
  completeOnboarding,
  getOnboardingReview,
  getOnboardingSession,
  queueOnboardingDiscovery,
  skipOnboardingDatabase,
  startOnboardingSession,
  submitOnboardingBasics,
  submitOnboardingDatabase,
  submitOnboardingRepository,
  validateRepositoryPath,
} from '../api/spaClient';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import type { OnboardingReview, OnboardingSession } from '../types/spa';

const STEPS = [
  { number: 1, label: 'Basics', key: 'ApplicationBasics' },
  { number: 2, label: 'Code', key: 'ConnectCode' },
  { number: 3, label: 'Database', key: 'ConnectDatabase' },
  { number: 4, label: 'Discovery', key: 'RunDiscovery' },
  { number: 5, label: 'Review', key: 'ReviewExport' },
  { number: 6, label: 'Done', key: 'Complete' },
];

interface OnboardingWizardAppProps {
  initialSessionId?: string;
}

export function OnboardingWizardApp({ initialSessionId }: OnboardingWizardAppProps) {
  const [session, setSession] = useState<OnboardingSession | null>(null);
  const [review, setReview] = useState<OnboardingReview | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const [basics, setBasics] = useState({
    name: '',
    businessDomain: '',
    purpose: '',
    riskSensitiveAreas: '',
    ownerTeamName: '',
  });
  const [repository, setRepository] = useState({
    repositoryName: '',
    repositoryPath: '',
    defaultBranch: 'main',
  });
  const [database, setDatabase] = useState({
    connectionName: '',
    provider: 'Sqlite',
    connectionString: 'Data Source=enhancementhub.db',
    isReadOnly: true,
  });
  const [pathValidation, setPathValidation] = useState<string | null>(null);

  const currentStepNumber = useMemo(() => {
    const match = STEPS.find((step) => step.key === session?.currentStep);
    return match?.number ?? 1;
  }, [session?.currentStep]);

  const refreshSession = useCallback(async (sessionId: string) => {
    const updated = await getOnboardingSession(sessionId);
    setSession(updated);
    return updated;
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function init() {
      setLoading(true);
      setError(null);
      try {
        const initial = initialSessionId
          ? await getOnboardingSession(initialSessionId)
          : await startOnboardingSession();
        if (!cancelled) {
          setSession(initial);
          if (initial.applicationName) {
            setBasics((prev) => ({ ...prev, name: initial.applicationName ?? prev.name }));
          }
        }
      } catch {
        if (!cancelled) {
          setError('Failed to start onboarding session.');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void init();
    return () => {
      cancelled = true;
    };
  }, [initialSessionId]);

  useEffect(() => {
    if (!session || session.currentStep !== 'ReviewExport' || !session.applicationId) {
      setReview(null);
      return;
    }

    let cancelled = false;

    async function loadReview() {
      try {
        const data = await getOnboardingReview(session!.id);
        if (!cancelled) {
          setReview(data);
        }
      } catch {
        if (!cancelled) {
          setError('Failed to load onboarding review.');
        }
      }
    }

    void loadReview();
    return () => {
      cancelled = true;
    };
  }, [session]);

  useEffect(() => {
    if (
      !session
      || session.currentStep !== 'RunDiscovery'
      || !['Queued', 'Running'].includes(session.discoveryJobState)
    ) {
      return;
    }

    const timer = window.setInterval(() => {
      void refreshSession(session.id).then(async (updated) => {
        if (updated.discoveryJobState === 'Completed' && updated.currentStep === 'RunDiscovery') {
          const advanced = await advanceOnboardingToReview(updated.id);
          setSession(advanced);
          setSuccess('Discovery completed. Review your application below.');
        }
      });
    }, 3000);

    return () => window.clearInterval(timer);
  }, [session, refreshSession]);

  async function runStep<T>(action: () => Promise<T>, onSuccess?: (result: T) => void) {
    setSubmitting(true);
    setError(null);
    setSuccess(null);
    try {
      const result = await action();
      onSuccess?.(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.');
    } finally {
      setSubmitting(false);
    }
  }

  async function onSubmitBasics(event: FormEvent) {
    event.preventDefault();
    if (!session) {
      return;
    }

    await runStep(
      () =>
        submitOnboardingBasics(session.id, {
          name: basics.name,
          businessDomain: basics.businessDomain || undefined,
          purpose: basics.purpose || undefined,
          riskSensitiveAreas: basics.riskSensitiveAreas || undefined,
          ownerTeamName: basics.ownerTeamName || undefined,
        }),
      (updated) => setSession(updated),
    );
  }

  async function onSubmitRepository(event: FormEvent) {
    event.preventDefault();
    if (!session) {
      return;
    }

    await runStep(
      () => submitOnboardingRepository(session.id, repository),
      (updated) => setSession(updated),
    );
  }

  async function onSubmitDatabase(event: FormEvent) {
    event.preventDefault();
    if (!session) {
      return;
    }

    await runStep(
      () => submitOnboardingDatabase(session.id, database),
      (updated) => setSession(updated),
    );
  }

  if (loading || !session) {
    return (
      <div aria-busy="true">
        <p className="text-muted" role="status">
          Starting wizard…
        </p>
        <LoadingSkeleton />
      </div>
    );
  }

  return (
    <div aria-live="polite">
      <div className="mb-4">
        <div className="d-flex flex-wrap gap-2">
          {STEPS.map((step) => (
            <span
              key={step.key}
              className={`badge ${step.number <= currentStepNumber ? 'text-bg-primary' : 'text-bg-secondary'}`}
            >
              {step.number}. {step.label}
            </span>
          ))}
        </div>
      </div>

      {session.wizardError || error ? (
        <div className="alert alert-danger" role="alert">
          {error ?? session.wizardError}
        </div>
      ) : null}
      {success ? (
        <div className="alert alert-success" role="status">
          {success}
        </div>
      ) : null}

      <div className="card-panel p-4">
        {session.currentStep === 'ApplicationBasics' ? (
          <form onSubmit={(event) => void onSubmitBasics(event)}>
            <h2 className="h4 mb-3">Step 1 — Application basics</h2>
            <div className="row g-3">
              <div className="col-md-6">
                <label className="form-label">Application name</label>
                <input
                  className="form-control"
                  required
                  value={basics.name}
                  onChange={(event) => setBasics({ ...basics, name: event.target.value })}
                />
              </div>
              <div className="col-md-6">
                <label className="form-label">Business domain</label>
                <input
                  className="form-control"
                  value={basics.businessDomain}
                  onChange={(event) => setBasics({ ...basics, businessDomain: event.target.value })}
                />
              </div>
              <div className="col-12">
                <label className="form-label">Purpose</label>
                <textarea
                  className="form-control"
                  rows={2}
                  value={basics.purpose}
                  onChange={(event) => setBasics({ ...basics, purpose: event.target.value })}
                />
              </div>
            </div>
            <button type="submit" className="btn btn-primary mt-4" disabled={submitting}>
              Continue to code connection
            </button>
          </form>
        ) : null}

        {session.currentStep === 'ConnectCode' ? (
          <form onSubmit={(event) => void onSubmitRepository(event)}>
            <h2 className="h4 mb-3">Step 2 — Connect code (local path)</h2>
            <p className="text-muted">
              For ZIP upload or GitHub App clone, use the{' '}
              <a href={`/Onboarding/Wizard/${session.id}`}>classic wizard</a>.
            </p>
            <div className="row g-3">
              <div className="col-md-6">
                <label className="form-label">Repository name</label>
                <input
                  className="form-control"
                  required
                  value={repository.repositoryName}
                  onChange={(event) => setRepository({ ...repository, repositoryName: event.target.value })}
                />
              </div>
              <div className="col-md-6">
                <label className="form-label">Default branch</label>
                <input
                  className="form-control"
                  value={repository.defaultBranch}
                  onChange={(event) => setRepository({ ...repository, defaultBranch: event.target.value })}
                />
              </div>
              <div className="col-12">
                <label className="form-label">Local repository path</label>
                <input
                  className="form-control"
                  required
                  value={repository.repositoryPath}
                  onChange={(event) => setRepository({ ...repository, repositoryPath: event.target.value })}
                />
              </div>
            </div>
            <div className="d-flex flex-wrap gap-2 mt-3">
              <button
                type="button"
                className="btn btn-outline-secondary"
                disabled={submitting || !repository.repositoryPath}
                onClick={() =>
                  void runStep(() => validateRepositoryPath(repository.repositoryPath), (result) =>
                    setPathValidation(
                      result.isValid
                        ? `Valid — ${result.csharpFileCount} C# files, ${result.controllerCount} controllers`
                        : result.errorMessage ?? 'Path is not valid',
                    ),
                  )
                }
              >
                Validate path
              </button>
              <button type="submit" className="btn btn-primary" disabled={submitting}>
                Continue to database
              </button>
            </div>
            {pathValidation ? <p className="small text-muted mt-2 mb-0">{pathValidation}</p> : null}
          </form>
        ) : null}

        {session.currentStep === 'ConnectDatabase' ? (
          <div>
            <h2 className="h4 mb-3">Step 3 — Connect database</h2>
            <form onSubmit={(event) => void onSubmitDatabase(event)}>
              <div className="row g-3">
                <div className="col-md-6">
                  <label className="form-label">Connection name</label>
                  <input
                    className="form-control"
                    required
                    value={database.connectionName}
                    onChange={(event) => setDatabase({ ...database, connectionName: event.target.value })}
                  />
                </div>
                <div className="col-md-6">
                  <label className="form-label">Provider</label>
                  <select
                    className="form-select"
                    value={database.provider}
                    onChange={(event) => setDatabase({ ...database, provider: event.target.value })}
                  >
                    <option value="Sqlite">SQLite</option>
                    <option value="PostgreSql">PostgreSQL</option>
                    <option value="SqlServer">SQL Server</option>
                  </select>
                </div>
                <div className="col-12">
                  <label className="form-label">Connection string</label>
                  <input
                    className="form-control"
                    required
                    value={database.connectionString}
                    onChange={(event) => setDatabase({ ...database, connectionString: event.target.value })}
                  />
                </div>
              </div>
              <div className="d-flex flex-wrap gap-2 mt-4">
                <button type="submit" className="btn btn-primary" disabled={submitting}>
                  Save and continue
                </button>
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  disabled={submitting}
                  onClick={() =>
                    void runStep(() => skipOnboardingDatabase(session.id), (updated) => setSession(updated))
                  }
                >
                  Skip database
                </button>
              </div>
            </form>
          </div>
        ) : null}

        {session.currentStep === 'RunDiscovery' ? (
          <div>
            <h2 className="h4 mb-3">Step 4 — Run discovery</h2>
            <p className="text-muted mb-3">
              Status: <strong>{session.discoveryJobState}</strong>
              {session.discoveryStatus ? ` — ${session.discoveryStatus}` : ''}
            </p>
            {session.discoveryJobState === 'None' ? (
              <button
                type="button"
                className="btn btn-primary"
                disabled={submitting}
                onClick={() =>
                  void runStep(() => queueOnboardingDiscovery(session.id), (updated) => {
                    setSession(updated);
                    setSuccess('Discovery queued. This page will refresh automatically.');
                  })
                }
              >
                Queue discovery
              </button>
            ) : null}
            {session.discoveryJobState === 'Completed' && session.currentStep === 'RunDiscovery' ? (
              <button
                type="button"
                className="btn btn-primary"
                disabled={submitting}
                onClick={() =>
                  void runStep(() => advanceOnboardingToReview(session.id), (updated) => setSession(updated))
                }
              >
                Continue to review
              </button>
            ) : null}
          </div>
        ) : null}

        {session.currentStep === 'ReviewExport' ? (
          <div>
            <h2 className="h4 mb-3">Step 5 — Review</h2>
            {review ? (
              <dl className="row">
                <dt className="col-sm-4">Application</dt>
                <dd className="col-sm-8">{review.applicationName}</dd>
                <dt className="col-sm-4">Repositories</dt>
                <dd className="col-sm-8">{review.repositoryCount}</dd>
                <dt className="col-sm-4">Database connections</dt>
                <dd className="col-sm-8">{review.databaseConnectionCount}</dd>
                <dt className="col-sm-4">Graph nodes / edges</dt>
                <dd className="col-sm-8">
                  {review.graphNodeCount} / {review.graphEdgeCount}
                </dd>
                <dt className="col-sm-4">Drift findings</dt>
                <dd className="col-sm-8">{review.driftFindingCount}</dd>
              </dl>
            ) : (
              <LoadingSkeleton />
            )}
            <button
              type="button"
              className="btn btn-primary mt-3"
              disabled={submitting}
              onClick={() =>
                void runStep(() => completeOnboarding(session.id), (updated) => {
                  setSession(updated);
                  setSuccess('Onboarding complete!');
                })
              }
            >
              Mark complete
            </button>
          </div>
        ) : null}

        {session.currentStep === 'Complete' ? (
          <div className="text-center py-3">
            <h2 className="h4">Setup complete</h2>
            <p className="text-muted">Your application is ready for enhancement requests and system intelligence.</p>
            <div className="d-flex justify-content-center gap-2">
              <a href="/" className="btn btn-primary">
                Go to dashboard
              </a>
              {session.applicationId ? (
                <a href={`/Spa/SystemMap?applicationId=${session.applicationId}`} className="btn btn-outline-secondary">
                  View system map
                </a>
              ) : null}
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}

import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import {
  advanceOnboardingToReview,
  completeOnboarding,
  getGitHubAppStatus,
  getOnboardingExportDocsUrl,
  getOnboardingReview,
  getOnboardingSession,
  queueOnboardingDiscovery,
  skipOnboardingDatabase,
  startOnboardingSession,
  submitOnboardingBasics,
  submitOnboardingDatabase,
} from '../api/spaClient';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { OnboardingCodeStep, OnboardingDatabaseStep } from '../components/OnboardingAdvancedSteps';
import type { GitHubAppStatus, OnboardingReview, OnboardingSession } from '../types/spa';

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
    deploymentNotes: '',
  });
  const [database, setDatabase] = useState({
    connectionName: '',
    provider: 'Sqlite',
    connectionString: 'Data Source=enhancementhub.db',
    isReadOnly: true,
  });
  const [githubStatus, setGitHubStatus] = useState<GitHubAppStatus | null>(null);

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
    void getGitHubAppStatus()
      .then(setGitHubStatus)
      .catch(() => setGitHubStatus(null));
  }, []);

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
          deploymentNotes: basics.deploymentNotes || undefined,
        }),
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

  function onSkipDatabase() {
    if (!session) {
      return;
    }

    void runStep(() => skipOnboardingDatabase(session.id), (updated) => setSession(updated));
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
      <div className="mb-4" role="list" aria-label="Onboarding steps">
        <div className="d-flex flex-wrap gap-2">
          {STEPS.map((step) => {
            const isCurrent = step.number === currentStepNumber;
            return (
              <span
                key={step.key}
                role="listitem"
                aria-current={isCurrent ? 'step' : undefined}
                className={`badge ${step.number <= currentStepNumber ? 'text-bg-primary' : 'text-bg-secondary'}`}
              >
                {step.number}. {step.label}
              </span>
            );
          })}
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
              <div className="col-md-6">
                <label className="form-label">Risk-sensitive areas</label>
                <input
                  className="form-control"
                  value={basics.riskSensitiveAreas}
                  onChange={(event) => setBasics({ ...basics, riskSensitiveAreas: event.target.value })}
                />
              </div>
              <div className="col-md-6">
                <label className="form-label">Owner team</label>
                <input
                  className="form-control"
                  value={basics.ownerTeamName}
                  onChange={(event) => setBasics({ ...basics, ownerTeamName: event.target.value })}
                />
              </div>
              <div className="col-12">
                <label className="form-label">Deployment &amp; infrastructure notes</label>
                <textarea
                  className="form-control"
                  rows={3}
                  placeholder="e.g. Azure App Service (P1v3), prefer existing Worker over new Azure Functions; avoid new paid SaaS"
                  value={basics.deploymentNotes}
                  onChange={(event) => setBasics({ ...basics, deploymentNotes: event.target.value })}
                />
                <div className="form-text">
                  Used by AI analysis to respect hosting and cost constraints when recommending changes.
                </div>
              </div>
            </div>
            <button type="submit" className="btn btn-primary mt-4" disabled={submitting}>
              Continue to code connection
            </button>
          </form>
        ) : null}

        {session.currentStep === 'ConnectCode' ? (
          <OnboardingCodeStep
            session={session}
            submitting={submitting}
            githubStatus={githubStatus}
            onSessionUpdated={setSession}
            onError={setError}
            runStep={runStep}
          />
        ) : null}

        {session.currentStep === 'ConnectDatabase' ? (
          <OnboardingDatabaseStep
            session={session}
            submitting={submitting}
            database={database}
            setDatabase={setDatabase}
            onSessionUpdated={setSession}
            onSkip={onSkipDatabase}
            onSubmitDirect={(event) => void onSubmitDatabase(event)}
            runStep={runStep}
          />
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
            <div className="d-flex flex-wrap gap-2 mt-3">
              <a className="btn btn-outline-secondary" href={getOnboardingExportDocsUrl(session.id)}>
                Export documentation
              </a>
              <button
                type="button"
                className="btn btn-primary"
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

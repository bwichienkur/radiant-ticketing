import { FormEvent, useCallback, useEffect, useState } from 'react';
import {
  advanceDeliveryPastPr,
  deployProduction,
  getDeliveryRun,
  rollbackProduction,
  signUat,
  startDeliveryRun,
} from '../api/spaClient';
import {
  AlertBanner,
  ConfirmDialog,
  LoadingState,
  SectionCard,
  useToast,
} from '../components/ui';
import type { EnhancementDeliveryRun } from '../types/spa';

interface DeliveryRunPanelProps {
  requestId: string;
  requestStatus: string;
  desiredOutcome: string;
}

type ConfirmKind = 'deploy' | 'rollback' | null;

export function DeliveryRunPanel({ requestId, requestStatus, desiredOutcome }: DeliveryRunPanelProps) {
  const toast = useToast();
  const [run, setRun] = useState<EnhancementDeliveryRun | null>(null);
  const [loading, setLoading] = useState(true);
  const [uatNotes, setUatNotes] = useState('');
  const [rollbackReason, setRollbackReason] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [confirmKind, setConfirmKind] = useState<ConfirmKind>(null);

  const reload = useCallback(async () => {
    try {
      const data = await getDeliveryRun(requestId);
      setRun(data);
    } catch {
      setRun(null);
    } finally {
      setLoading(false);
    }
  }, [requestId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  useEffect(() => {
    if (!run || run.phase === 'Completed' || run.phase === 'Failed') {
      return;
    }

    const timer = window.setInterval(() => {
      void reload();
    }, 5000);

    return () => window.clearInterval(timer);
  }, [run?.phase, reload]);

  async function handleStart() {
    setSubmitting(true);
    try {
      setRun(await startDeliveryRun(requestId));
      toast.success('Delivery started', 'Automated implementation is in progress.');
    } catch {
      toast.danger('Could not start delivery', 'Check delivery configuration and try again.');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleAdvancePr() {
    setSubmitting(true);
    try {
      setRun(await advanceDeliveryPastPr(requestId));
      toast.info('Continuing', 'Deploying to the test environment.');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleUat(event: FormEvent, approved: boolean) {
    event.preventDefault();
    setSubmitting(true);
    try {
      setRun(await signUat(requestId, approved, uatNotes || undefined));
      toast.success(
        approved ? 'UAT approved' : 'UAT rejected',
        approved ? 'Production scheduling started.' : 'The delivery run will need changes.',
      );
    } finally {
      setSubmitting(false);
    }
  }

  async function handleDeployProduction() {
    setSubmitting(true);
    try {
      setRun(await deployProduction(requestId));
      toast.success('Production deploy started', 'Monitor the timeline for completion.');
      setConfirmKind(null);
    } catch {
      toast.danger('Deploy failed', 'Could not deploy to production.');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleRollbackProduction() {
    setSubmitting(true);
    try {
      setRun(await rollbackProduction(requestId, rollbackReason || undefined));
      toast.warning('Rollback triggered', 'Restoring the previous production version.');
      setConfirmKind(null);
    } catch {
      toast.danger('Rollback failed', 'Could not roll back production.');
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return <LoadingState label="Loading delivery status…" />;
  }

  const canStart =
    !run && (requestStatus === 'Approved' || requestStatus === 'ReadyForDevelopment');

  return (
    <SectionCard
      title="Delivery progress"
      ariaLabel="Delivery progress"
      actions={run?.isSimulation ? <span className="badge text-bg-secondary">Simulation mode</span> : undefined}
    >
      <p className="small text-muted mb-3">
        Automated implementation, test deploy, QA evidence, and UAT.
        {run ? (
          <>
            {' '}
            Current phase: <strong>{formatPhase(run.phase)}</strong>
          </>
        ) : null}
      </p>

      {canStart ? (
        <button type="button" className="btn btn-primary btn-sm" disabled={submitting} onClick={() => void handleStart()}>
          Start automated delivery
        </button>
      ) : null}

      {!run ? (
        <p className="small text-muted mb-0 mt-2">No delivery run yet.</p>
      ) : (
        <>
          <dl className="row small mb-3">
            <dt className="col-sm-3">Phase</dt>
            <dd className="col-sm-9">{formatPhase(run.phase)}</dd>
            {run.branchName ? (
              <>
                <dt className="col-sm-3">Branch</dt>
                <dd className="col-sm-9">{run.branchName}</dd>
              </>
            ) : null}
            {run.pullRequestUrl ? (
              <>
                <dt className="col-sm-3">Pull request</dt>
                <dd className="col-sm-9">
                  <a href={run.pullRequestUrl} target="_blank" rel="noreferrer">
                    #{run.pullRequestNumber ?? 'PR'}
                  </a>
                </dd>
              </>
            ) : null}
            {run.testUrl ? (
              <>
                <dt className="col-sm-3">Test environment</dt>
                <dd className="col-sm-9">
                  <a href={run.testUrl} target="_blank" rel="noreferrer">
                    {run.testUrl}
                  </a>
                </dd>
              </>
            ) : null}
            {run.prodScheduledAt ? (
              <>
                <dt className="col-sm-3">Production scheduled</dt>
                <dd className="col-sm-9">{new Date(run.prodScheduledAt).toLocaleString()}</dd>
              </>
            ) : null}
          </dl>

          {run.phase === 'AwaitingPullRequestReview' ? (
            <button
              type="button"
              className="btn btn-outline-primary btn-sm mb-3"
              disabled={submitting}
              onClick={() => void handleAdvancePr()}
            >
              PR approved — deploy to test
            </button>
          ) : null}

          {run.testCaseResults.length > 0 ? (
            <div className="mb-3">
              <h3 className="h6">Test cases</h3>
              <ul className="small list-unstyled mb-0">
                {run.testCaseResults.map((testCase) => (
                  <li key={testCase.testCaseId} className="mb-1">
                    <span className={testCase.passed ? 'text-success' : 'text-danger'}>
                      {testCase.passed ? '✓' : '✗'}
                    </span>{' '}
                    {testCase.title}
                    {testCase.isRegressionCase ? (
                      <span className="badge text-bg-light ms-1">Regression</span>
                    ) : (
                      <span className="badge text-bg-info ms-1">New</span>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          ) : null}

          {run.qaSteps.length > 0 ? (
            <div className="mb-3">
              <h3 className="h6">QA steps</h3>
              <ul className="small mb-2">
                {run.qaSteps.map((step) => (
                  <li key={step.step}>
                    <span className={step.passed ? 'text-success' : 'text-danger'}>
                      {step.passed ? '✓' : '✗'}
                    </span>{' '}
                    {step.step}
                  </li>
                ))}
              </ul>
              <div className="d-flex flex-wrap gap-2">
                {run.qaReportUrl ? (
                  <a className="btn btn-sm btn-outline-secondary" href={run.qaReportUrl} target="_blank" rel="noreferrer">
                    QA report
                  </a>
                ) : null}
                {run.qaVideoUrl ? (
                  <a className="btn btn-sm btn-outline-secondary" href={run.qaVideoUrl} target="_blank" rel="noreferrer">
                    QA walkthrough
                  </a>
                ) : null}
              </div>
            </div>
          ) : null}

          {run.rollbackPlan ? (
            <div className="mb-3 border rounded p-3 bg-light-subtle">
              <h3 className="h6">Rollback plan</h3>
              <p className="small mb-0">{run.rollbackPlan}</p>
            </div>
          ) : null}

          {run.canDeployToProduction ? (
            <button
              type="button"
              className="btn btn-success btn-sm mb-3"
              disabled={submitting}
              onClick={() => setConfirmKind('deploy')}
            >
              Deploy to production
            </button>
          ) : null}

          {run.canRollbackProduction ? (
            <div className="mb-3 border rounded p-3">
              <h3 className="h6">Rollback production</h3>
              <p className="small text-muted">
                Restore the previous production version
                {run.rollbackTargetCommitSha ? ` (${run.rollbackTargetCommitSha})` : ''}.
              </p>
              <label className="form-label small" htmlFor="rollback-reason">
                Reason (optional)
              </label>
              <input
                id="rollback-reason"
                className="form-control form-control-sm mb-2"
                value={rollbackReason}
                onChange={(event) => setRollbackReason(event.target.value)}
              />
              <button
                type="button"
                className="btn btn-outline-danger btn-sm"
                disabled={submitting}
                onClick={() => setConfirmKind('rollback')}
              >
                Roll back production
              </button>
            </div>
          ) : null}

          {run.postDeploySmokePassed === false ? (
            <AlertBanner variant="warning" className="py-2 small mb-3">
              Post-deploy smoke tests failed. Consider rolling back production.
            </AlertBanner>
          ) : null}

          {run.phase === 'AwaitingUat' ? (
            <form className="border rounded p-3 bg-light-subtle" onSubmit={(e) => void handleUat(e, true)}>
              <h3 className="h6">User acceptance testing</h3>
              <p className="small text-muted">Confirm the test environment matches your goal:</p>
              <p className="small mb-2">
                <strong>Success looks like:</strong> {desiredOutcome}
              </p>
              <label className="form-label small" htmlFor="uat-notes">
                Notes (optional)
              </label>
              <textarea
                id="uat-notes"
                className="form-control form-control-sm mb-2"
                rows={2}
                value={uatNotes}
                onChange={(event) => setUatNotes(event.target.value)}
              />
              <div className="d-flex gap-2 flex-wrap">
                <button type="submit" className="btn btn-success btn-sm" disabled={submitting}>
                  Approve for production
                </button>
                <button
                  type="button"
                  className="btn btn-outline-danger btn-sm"
                  disabled={submitting}
                  onClick={(e) => void handleUat(e, false)}
                >
                  Needs changes
                </button>
              </div>
            </form>
          ) : null}

          {run.timeline.length > 0 ? (
            <details className="mt-3">
              <summary className="small fw-semibold">Timeline</summary>
              <ul className="small text-muted mb-0 mt-2">
                {run.timeline.map((event) => (
                  <li key={`${event.occurredAt}-${event.message}`}>
                    {new Date(event.occurredAt).toLocaleString()} — {event.message}
                  </li>
                ))}
              </ul>
            </details>
          ) : null}

          {run.lastError ? (
            <AlertBanner variant="warning" className="py-2 small mt-3 mb-0">
              {run.lastError}
            </AlertBanner>
          ) : null}
        </>
      )}

      <ConfirmDialog
        open={confirmKind === 'deploy'}
        title="Deploy to production?"
        message="This will promote the tested build to your production environment. Ensure UAT sign-off is complete."
        confirmLabel="Deploy now"
        variant="primary"
        loading={submitting}
        onConfirm={() => void handleDeployProduction()}
        onCancel={() => setConfirmKind(null)}
      />
      <ConfirmDialog
        open={confirmKind === 'rollback'}
        title="Roll back production?"
        message="This restores the previous production version. Use only when the current release is causing issues."
        confirmLabel="Roll back"
        variant="danger"
        loading={submitting}
        onConfirm={() => void handleRollbackProduction()}
        onCancel={() => setConfirmKind(null)}
      />
    </SectionCard>
  );
}

function formatPhase(phase: string): string {
  return phase.replace(/([a-z])([A-Z])/g, '$1 $2');
}

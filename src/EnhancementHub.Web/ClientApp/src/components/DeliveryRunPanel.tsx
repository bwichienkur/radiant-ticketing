import { FormEvent, useCallback, useEffect, useState } from 'react';
import {
  advanceDeliveryPastPr,
  deployProduction,
  getDeliveryRun,
  rollbackProduction,
  signUat,
  startDeliveryRun,
} from '../api/spaClient';
import type { EnhancementDeliveryRun } from '../types/spa';

interface DeliveryRunPanelProps {
  requestId: string;
  requestStatus: string;
  desiredOutcome: string;
}

export function DeliveryRunPanel({ requestId, requestStatus, desiredOutcome }: DeliveryRunPanelProps) {
  const [run, setRun] = useState<EnhancementDeliveryRun | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionMessage, setActionMessage] = useState<string | null>(null);
  const [uatNotes, setUatNotes] = useState('');
  const [rollbackReason, setRollbackReason] = useState('');
  const [submitting, setSubmitting] = useState(false);

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
    setActionMessage(null);
    try {
      setRun(await startDeliveryRun(requestId));
      setActionMessage('Delivery started.');
    } catch {
      setActionMessage('Could not start delivery.');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleAdvancePr() {
    setSubmitting(true);
    try {
      setRun(await advanceDeliveryPastPr(requestId));
      setActionMessage('Continuing to test deployment.');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleUat(event: FormEvent, approved: boolean) {
    event.preventDefault();
    setSubmitting(true);
    try {
      setRun(await signUat(requestId, approved, uatNotes || undefined));
      setActionMessage(approved ? 'UAT approved — production scheduling started.' : 'UAT rejected.');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleDeployProduction() {
    setSubmitting(true);
    setActionMessage(null);
    try {
      setRun(await deployProduction(requestId));
      setActionMessage('Production deploy started.');
    } catch {
      setActionMessage('Could not deploy to production.');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleRollbackProduction() {
    setSubmitting(true);
    setActionMessage(null);
    try {
      setRun(await rollbackProduction(requestId, rollbackReason || undefined));
      setActionMessage('Production rollback triggered.');
    } catch {
      setActionMessage('Could not roll back production.');
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return null;
  }

  const canStart =
    !run && (requestStatus === 'Approved' || requestStatus === 'ReadyForDevelopment');

  return (
    <section className="card-panel p-4 mb-3" aria-label="Delivery progress">
      <div className="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
        <div>
          <h2 className="h6 mb-1">Delivery progress</h2>
          <p className="small text-muted mb-0">
            Automated implementation, test deploy, QA evidence, and UAT.
          </p>
        </div>
        {run?.isSimulation ? <span className="badge text-bg-secondary">Simulation mode</span> : null}
      </div>

      {actionMessage ? (
        <div className="alert alert-info py-2 small" role="status">
          {actionMessage}
        </div>
      ) : null}

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
              onClick={() => void handleDeployProduction()}
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
                onClick={() => void handleRollbackProduction()}
              >
                Roll back production
              </button>
            </div>
          ) : null}

          {run.postDeploySmokePassed === false ? (
            <div className="alert alert-warning py-2 small mb-3" role="alert">
              Post-deploy smoke tests failed. Consider rolling back production.
            </div>
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
            <div className="alert alert-warning py-2 small mt-3 mb-0" role="alert">
              {run.lastError}
            </div>
          ) : null}
        </>
      )}
    </section>
  );
}

function formatPhase(phase: string): string {
  return phase.replace(/([a-z])([A-Z])/g, '$1 $2');
}

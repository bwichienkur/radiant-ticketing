import { useMemo, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { submitProductFeedback } from '../api/spaClient';

const WORKFLOW_LABELS: Record<string, string> = {
  dashboard: 'Dashboard',
  'request-list': 'Request list',
  'create-request': 'Create request',
  'request-detail': 'Request detail',
  approval: 'Approval queue',
  onboarding: 'Onboarding wizard',
  'system-map': 'System map',
  applications: 'Applications',
  'schema-drift': 'Schema drift',
  repositories: 'Repositories',
  audit: 'Audit log',
  search: 'Global search',
  'portfolio-health': 'Portfolio health',
  settings: 'Settings',
  admin: 'Admin console',
};

function resolveWorkflowKey(pathname: string): string {
  if (pathname === '/' || pathname === '/Index') {
    return 'dashboard';
  }

  if (pathname.startsWith('/Spa/RequestList')) {
    return 'request-list';
  }

  if (pathname.startsWith('/Spa/CreateRequest')) {
    return 'create-request';
  }

  if (pathname.startsWith('/Spa/RequestDetail')) {
    return 'request-detail';
  }

  if (pathname.startsWith('/Spa/ApprovalQueue')) {
    return 'approval';
  }

  if (pathname.startsWith('/Spa/OnboardingWizard')) {
    return 'onboarding';
  }

  if (pathname.startsWith('/Spa/SystemMap')) {
    return 'system-map';
  }

  if (pathname.startsWith('/Spa/Applications')) {
    return 'applications';
  }

  if (pathname.startsWith('/Spa/SchemaDrift')) {
    return 'schema-drift';
  }

  if (pathname.startsWith('/Spa/Repositories')) {
    return 'repositories';
  }

  if (pathname.startsWith('/Spa/Audit')) {
    return 'audit';
  }

  if (pathname.startsWith('/Spa/Search')) {
    return 'search';
  }

  if (pathname.startsWith('/Spa/PortfolioHealth')) {
    return 'portfolio-health';
  }

  if (pathname.startsWith('/Spa/Settings')) {
    return 'settings';
  }

  if (pathname.startsWith('/Spa/Admin')) {
    return 'admin';
  }

  return 'other';
}

export function FeedbackWidget() {
  const location = useLocation();
  const workflowKey = useMemo(
    () => resolveWorkflowKey(location.pathname),
    [location.pathname],
  );
  const workflowLabel = WORKFLOW_LABELS[workflowKey] ?? 'This workflow';

  const [isOpen, setIsOpen] = useState(false);
  const [npsScore, setNpsScore] = useState<number | null>(null);
  const [comment, setComment] = useState('');
  const [status, setStatus] = useState<'idle' | 'submitting' | 'submitted' | 'error'>('idle');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();

    if (npsScore === null) {
      setErrorMessage('Select a score from 0 (not likely) to 10 (very likely).');
      return;
    }

    setStatus('submitting');
    setErrorMessage(null);

    try {
      await submitProductFeedback({
        workflowKey,
        npsScore,
        comment: comment.trim() || undefined,
      });
      setStatus('submitted');
      setComment('');
      setNpsScore(null);
    } catch (error) {
      setStatus('error');
      setErrorMessage(error instanceof Error ? error.message : 'Unable to submit feedback.');
    }
  }

  function handleClose() {
    setIsOpen(false);
    if (status === 'submitted') {
      setStatus('idle');
    }
  }

  return (
    <div className="eh-feedback-widget" aria-live="polite">
      {!isOpen ? (
        <button
          type="button"
          className="btn btn-outline-primary btn-sm eh-feedback-trigger"
          onClick={() => setIsOpen(true)}
          aria-expanded="false"
          aria-controls="eh-feedback-panel"
        >
          Feedback
        </button>
      ) : (
        <div
          id="eh-feedback-panel"
          className="card-panel p-3 eh-feedback-panel"
          role="dialog"
          aria-label="Product feedback"
        >
          <div className="d-flex justify-content-between align-items-start gap-2 mb-2">
            <div>
              <strong className="small d-block">How is {workflowLabel} working for you?</strong>
              <span className="text-muted small">0 = not likely · 10 = very likely to recommend</span>
            </div>
            <button
              type="button"
              className="btn-close btn-sm"
              aria-label="Close feedback"
              onClick={handleClose}
            />
          </div>

          {status === 'submitted' ? (
            <p className="mb-0 small text-success">Thank you — your feedback was recorded.</p>
          ) : (
            <form onSubmit={handleSubmit}>
              <div className="d-flex flex-wrap gap-1 mb-2" role="group" aria-label="NPS score 0 to 10">
                {Array.from({ length: 11 }, (_, score) => (
                  <button
                    key={score}
                    type="button"
                    className={`btn btn-sm ${npsScore === score ? 'btn-primary' : 'btn-outline-secondary'}`}
                    aria-pressed={npsScore === score}
                    onClick={() => setNpsScore(score)}
                  >
                    {score}
                  </button>
                ))}
              </div>

              <label className="form-label small mb-1" htmlFor="eh-feedback-comment">
                Optional comments
              </label>
              <textarea
                id="eh-feedback-comment"
                className="form-control form-control-sm mb-2"
                rows={3}
                maxLength={2000}
                value={comment}
                onChange={(event) => setComment(event.target.value)}
                placeholder="What worked well or could be better?"
              />

              {errorMessage ? (
                <p className="small text-danger mb-2" role="alert">
                  {errorMessage}
                </p>
              ) : null}

              <div className="d-flex gap-2">
                <button
                  type="submit"
                  className="btn btn-primary btn-sm"
                  disabled={status === 'submitting'}
                >
                  {status === 'submitting' ? 'Sending…' : 'Submit'}
                </button>
                <button type="button" className="btn btn-outline-secondary btn-sm" onClick={handleClose}>
                  Cancel
                </button>
              </div>
            </form>
          )}
        </div>
      )}
    </div>
  );
}

import { useCallback, useEffect, useState } from 'react';
import {
  getApprovalRecommendation,
  getApprovalRequestDetail,
  listPendingApprovals,
  submitApprovalAction,
} from '../api/spaClient';
import {
  AlertBanner,
  ConfirmDialog,
  EmptyState,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  StatusBadge,
  useToast,
} from '../components/ui';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { formatApprovalAction, formatConfidenceLabel } from '../utils/requestLabels';
import type { ApprovalRecommendation, ApprovalRequestDetail, PendingApprovalItem } from '../types/spa';
import { SpaLink } from '../components/SpaLink';

interface ApprovalQueueAppProps {
  initialRequestId?: string;
}

type PendingAction = 'Approve' | 'RequestClarification' | 'Reject' | null;

export function ApprovalQueueApp({ initialRequestId }: ApprovalQueueAppProps) {
  const toast = useToast();
  const [pending, setPending] = useState<PendingApprovalItem[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(initialRequestId ?? null);
  const [selected, setSelected] = useState<ApprovalRequestDetail | null>(null);
  const [recommendation, setRecommendation] = useState<ApprovalRecommendation | null>(null);
  const [comments, setComments] = useState('');
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [actionInFlight, setActionInFlight] = useState(false);
  const [confirmAction, setConfirmAction] = useState<PendingAction>(null);

  const loadQueue = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await listPendingApprovals();
      setPending(items);
      if (items.length === 0) {
        setSelectedId(null);
        setSelected(null);
      } else if (!selectedId || !items.some((item) => item.id === selectedId)) {
        setSelectedId(items[0].id);
      }
    } catch {
      setError('Failed to load approval queue.');
    } finally {
      setLoading(false);
    }
  }, [selectedId]);

  useEffect(() => {
    void loadQueue();
  }, [loadQueue]);

  useEffect(() => {
    if (!selectedId) {
      setSelected(null);
      setRecommendation(null);
      return;
    }

    let cancelled = false;

    async function loadDetail() {
      setDetailLoading(true);
      try {
        const [detail, rec] = await Promise.all([
          getApprovalRequestDetail(selectedId!),
          getApprovalRecommendation(selectedId!),
        ]);
        if (!cancelled) {
          setSelected(detail);
          setRecommendation(rec);
        }
      } catch {
        if (!cancelled) {
          setError('Failed to load request details.');
        }
      } finally {
        if (!cancelled) {
          setDetailLoading(false);
        }
      }
    }

    void loadDetail();
    return () => {
      cancelled = true;
    };
  }, [selectedId]);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
        return;
      }

      if (pending.length === 0 || !selectedId) {
        return;
      }

      const index = pending.findIndex((item) => item.id === selectedId);
      if (event.key === 'j' || event.key === 'J') {
        event.preventDefault();
        setSelectedId(pending[Math.min(index + 1, pending.length - 1)].id);
      }

      if (event.key === 'k' || event.key === 'K') {
        event.preventDefault();
        setSelectedId(pending[Math.max(index - 1, 0)].id);
      }
    }

    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [pending, selectedId]);

  async function handleAction(actionType: string) {
    if (!selectedId || actionInFlight) {
      return;
    }

    setError(null);
    setActionInFlight(true);
    try {
      await submitApprovalAction(selectedId, actionType, comments || undefined);
      toast.success(
        formatApprovalAction(actionType),
        'Your decision was recorded.',
      );
      setComments('');
      setConfirmAction(null);
      await loadQueue();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to submit approval action.';
      setError(message);
      toast.danger('Action failed', message);
    } finally {
      setActionInFlight(false);
    }
  }

  function requestAction(actionType: PendingAction) {
    if (actionType === 'Reject') {
      setConfirmAction(actionType);
      return;
    }

    void handleAction(actionType!);
  }

  const latestAnalysis = selected?.analyses
    ?.slice()
    .sort((a, b) => b.version - a.version)[0];

  function recommendationVariant(rec: ApprovalRecommendation): 'success' | 'warning' | 'danger' | 'info' {
    switch (rec.recommendation) {
      case 'Approve':
        return 'success';
      case 'ApproveWithCare':
      case 'Caution':
        return 'warning';
      case 'Reject':
        return 'danger';
      default:
        return 'info';
    }
  }

  function recommendationLabel(rec: ApprovalRecommendation): string {
    switch (rec.recommendation) {
      case 'Approve':
        return 'Recommend approve';
      case 'ApproveWithCare':
        return 'Approve with care';
      case 'Caution':
        return 'Proceed with caution';
      case 'RequestClarification':
        return 'Ask for clarification';
      case 'Reject':
        return 'Consider declining';
      default:
        return 'Review guidance';
    }
  }

  function recommendationSourceLabel(source?: string): string | null {
    switch (source) {
      case 'Llm':
        return 'AI copilot';
      case 'HeuristicFallback':
        return 'Rule-based fallback';
      case 'Heuristic':
        return 'Rule-based';
      default:
        return null;
    }
  }

  if (loading) {
    return <LoadingState label="Loading approval queue…" />;
  }

  return (
    <div aria-live="polite">
      <PageHeader
        title="Requests waiting for your decision"
        description="Review each request, read the summary, and approve, reject, or ask for more information. Use J/K to move between items."
      />

      {error ? <ErrorState message={error} onRetry={() => void loadQueue()} className="mb-3" /> : null}

      <div className="approval-queue-layout row g-3 g-lg-4">
        <div className="col-lg-4">
          <nav className="card-panel approval-queue-nav" aria-label="Pending requests">
            <div className="card-header d-flex justify-content-between align-items-center">
              <span className="eh-section-title mb-0">Pending</span>
              <span className="badge text-bg-primary">{pending.length}</span>
            </div>
            {pending.length === 0 ? (
              <EmptyState
                title="All caught up"
                description="No requests need your decision right now. New submissions will appear here."
                icon="inbox"
                embedded
              />
            ) : (
              <ul className="list-group list-group-flush approval-queue-list" role="list">
                {pending.map((item) => (
                  <li key={item.id} className="list-group-item p-0" role="listitem">
                    <button
                      type="button"
                      className={`approval-queue-item list-group-item list-group-item-action w-100 text-start ${
                        selectedId === item.id ? 'active' : ''
                      }`}
                      aria-current={selectedId === item.id ? 'true' : undefined}
                      onClick={() => setSelectedId(item.id)}
                    >
                      <div className="d-flex justify-content-between gap-2">
                        <strong className="d-block">{item.title}</strong>
                        <StatusBadge risk={item.latestRiskLevel} />
                      </div>
                      <span className="d-block small approval-queue-meta">
                        {item.submittedByUserName ?? '—'} · {item.priority} · {item.daysInStatus ?? 0}d waiting
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </nav>
        </div>

        <div className="col-lg-8">
          {detailLoading ? (
            <LoadingSkeleton />
          ) : selected ? (
            <article className="card-panel p-3 p-md-4 approval-queue-detail">
              <div className="approval-decision-header">
                <div className="d-flex flex-wrap justify-content-between align-items-start gap-2 mb-2">
                  <div>
                    <h2 className="h5 mb-1">{selected.title}</h2>
                    <div className="small text-muted">
                      {selected.submittedByUserName} · {selected.priority} priority · {selected.department ?? '—'}
                      {latestAnalysis ? (
                        <>
                          {' '}
                          · <StatusBadge risk={latestAnalysis.riskLevel} />
                          <span className="ms-1" title="How confident the AI is in its assessment">
                            {formatConfidenceLabel(latestAnalysis.confidenceScore)}
                          </span>
                        </>
                      ) : null}
                    </div>
                  </div>
                  <SpaLink href={`/Spa/RequestDetail/${selected.id}`} className="btn btn-sm btn-outline-primary">
                    View details
                  </SpaLink>
                </div>
                {latestAnalysis?.featureSummary ? (
                  <p className="mb-0 small">
                    <strong>AI summary:</strong> {latestAnalysis.featureSummary}
                  </p>
                ) : null}
              </div>

              {recommendation ? (
                <AlertBanner
                  variant={recommendationVariant(recommendation)}
                  title={[
                    recommendationLabel(recommendation),
                    recommendationSourceLabel(recommendation.source),
                  ]
                    .filter(Boolean)
                    .join(' · ')}
                  className="mb-3"
                >
                  {recommendation.summary}
                </AlertBanner>
              ) : null}

              <details className="mb-3" open>
                <summary className="fw-semibold mb-2">Business context</summary>
                <p className="text-muted mb-0">{selected.businessDescription}</p>
              </details>

              <div className="approval-quick-actions mb-3 d-flex flex-wrap gap-2">
                <button
                  type="button"
                  className="btn btn-success approval-action-btn"
                  disabled={actionInFlight}
                  onClick={() => requestAction('Approve')}
                >
                  {actionInFlight ? 'Submitting…' : 'Approve request'}
                </button>
                <button
                  type="button"
                  className="btn btn-outline-warning approval-action-btn"
                  disabled={actionInFlight}
                  onClick={() => requestAction('RequestClarification')}
                >
                  {actionInFlight ? 'Submitting…' : 'Ask for more information'}
                </button>
                <button
                  type="button"
                  className="btn btn-outline-danger approval-action-btn"
                  disabled={actionInFlight}
                  onClick={() => requestAction('Reject')}
                >
                  Decline request
                </button>
              </div>

              <FormField
                id="approval-comments"
                label="Add a note for the requester (optional)"
                hint="Visible to the requester unless marked internal on the detail page."
              >
                <textarea
                  id="approval-comments"
                  className="form-control"
                  rows={3}
                  value={comments}
                  onChange={(event) => setComments(event.target.value)}
                  placeholder="Add context for your decision…"
                />
              </FormField>
            </article>
          ) : (
            <div className="card-panel p-4 text-muted" role="status">
              Select a request from the list to review it.
            </div>
          )}
        </div>
      </div>

      <ConfirmDialog
        open={confirmAction === 'Reject'}
        title="Decline this request?"
        message="The requester will be notified. You can still add an optional note above before confirming."
        confirmLabel="Decline request"
        variant="danger"
        loading={actionInFlight}
        onConfirm={() => void handleAction('Reject')}
        onCancel={() => setConfirmAction(null)}
      />
    </div>
  );
}

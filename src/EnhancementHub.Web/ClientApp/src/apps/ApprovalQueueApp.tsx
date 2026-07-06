import { useCallback, useEffect, useState } from 'react';
import {
  getApprovalRequestDetail,
  listPendingApprovals,
  submitApprovalAction,
} from '../api/spaClient';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { riskBadgeClass } from '../components/MissionControl';
import { formatApprovalAction, formatConfidenceLabel } from '../utils/requestLabels';
import type { ApprovalRequestDetail, PendingApprovalItem } from '../types/spa';

interface ApprovalQueueAppProps {
  initialRequestId?: string;
}

export function ApprovalQueueApp({ initialRequestId }: ApprovalQueueAppProps) {
  const [pending, setPending] = useState<PendingApprovalItem[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(initialRequestId ?? null);
  const [selected, setSelected] = useState<ApprovalRequestDetail | null>(null);
  const [comments, setComments] = useState('');
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [actionMessage, setActionMessage] = useState<string | null>(null);
  const [actionInFlight, setActionInFlight] = useState(false);

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
      return;
    }

    let cancelled = false;

    async function loadDetail() {
      setDetailLoading(true);
      try {
        const detail = await getApprovalRequestDetail(selectedId!);
        if (!cancelled) {
          setSelected(detail);
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
      if (pending.length === 0 || !selectedId) {
        return;
      }

      const index = pending.findIndex((item) => item.id === selectedId);
      if (event.key === 'j' || event.key === 'J') {
        event.preventDefault();
        const next = pending[Math.min(index + 1, pending.length - 1)];
        setSelectedId(next.id);
      }

      if (event.key === 'k' || event.key === 'K') {
        event.preventDefault();
        const prev = pending[Math.max(index - 1, 0)];
        setSelectedId(prev.id);
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
    setActionMessage(null);
    setActionInFlight(true);
    try {
      await submitApprovalAction(selectedId, actionType, comments || undefined);
      setActionMessage(`${formatApprovalAction(actionType)} — your decision was recorded.`);
      setComments('');
      await loadQueue();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit approval action.');
    } finally {
      setActionInFlight(false);
    }
  }

  const latestAnalysis = selected?.analyses
    ?.slice()
    .sort((a, b) => b.version - a.version)[0];

  if (loading) {
    return (
      <div aria-busy="true">
        <p className="text-muted" role="status">
          Loading approval queue…
        </p>
        <LoadingSkeleton />
      </div>
    );
  }

  return (
    <div aria-live="polite">
      <div className="page-header mb-4">
        <h1>Requests waiting for your decision</h1>
        <p className="mb-0 text-muted">
          Review each request, read the summary, and approve, reject, or ask for more information.
        </p>
      </div>

    <div className="approval-queue-layout row g-3 g-lg-4">
      <div className="col-lg-4">
        <nav className="card-panel approval-queue-nav" aria-label="Pending requests">
          <div className="card-header d-flex justify-content-between align-items-center">
            <span>Pending</span>
            <span className="badge text-bg-primary">{pending.length}</span>
          </div>
          {pending.length === 0 ? (
            <div className="empty-state py-4 px-3">
              <p className="mb-1 fw-semibold">All caught up</p>
              <p className="text-muted mb-0 small">
                No requests need your decision right now. New submissions will appear here.
              </p>
            </div>
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
                      {item.latestRiskLevel ? (
                        <span className={`badge ${riskBadgeClass(item.latestRiskLevel)} badge-status`}>
                          {item.latestRiskLevel}
                        </span>
                      ) : null}
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
        {error ? (
          <div className="alert alert-danger d-flex flex-wrap justify-content-between align-items-center gap-2" role="alert">
            <span>{error}</span>
            <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => void loadQueue()}>
              Retry
            </button>
          </div>
        ) : null}
        {actionMessage ? (
          <div className="alert alert-success" role="status">
            {actionMessage}
          </div>
        ) : null}

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
                        ·{' '}
                        <span className={`badge ${riskBadgeClass(latestAnalysis.riskLevel)} badge-status`}>
                          {latestAnalysis.riskLevel} risk
                        </span>
                        <span className="ms-1" title="How confident the AI is in its assessment">
                          {formatConfidenceLabel(latestAnalysis.confidenceScore)}
                        </span>
                      </>
                    ) : null}
                  </div>
                </div>
                <div className="d-flex gap-2">
                  <a href={`/Spa/RequestDetail/${selected.id}`} className="btn btn-sm btn-outline-primary">
                    View details
                  </a>
                </div>
              </div>
              {latestAnalysis?.featureSummary ? (
                <p className="mb-0 small">
                  <strong>AI summary:</strong> {latestAnalysis.featureSummary}
                </p>
              ) : null}
            </div>

            <details className="mb-3" open>
              <summary className="fw-semibold mb-2">Business context</summary>
              <p className="text-muted mb-0">{selected.businessDescription}</p>
            </details>

            <div className="approval-quick-actions mb-3 d-flex flex-wrap gap-2">
              <button
                type="button"
                className="btn btn-success approval-action-btn"
                disabled={actionInFlight}
                onClick={() => void handleAction('Approve')}
              >
                {actionInFlight ? 'Submitting…' : 'Approve request'}
              </button>
              <button
                type="button"
                className="btn btn-outline-warning approval-action-btn"
                disabled={actionInFlight}
                onClick={() => void handleAction('RequestClarification')}
              >
                {actionInFlight ? 'Submitting…' : 'Ask for more information'}
              </button>
              <button
                type="button"
                className="btn btn-outline-danger approval-action-btn"
                disabled={actionInFlight}
                onClick={() => void handleAction('Reject')}
              >
                {actionInFlight ? 'Submitting…' : 'Decline request'}
              </button>
            </div>

            <label className="form-label" htmlFor="approval-comments">
              Add a note for the requester (optional)
            </label>
            <textarea
              id="approval-comments"
              className="form-control mb-0"
              rows={3}
              value={comments}
              onChange={(event) => setComments(event.target.value)}
              placeholder="Add context for your decision…"
            />
          </article>
        ) : (
          <div className="card-panel p-4 text-muted">
            Select a request from the list to review it.
          </div>
        )}
      </div>
    </div>
    </div>
  );
}

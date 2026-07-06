import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import {
  getApprovalHistory,
  getRequestAnalysis,
  getRequestDetail,
  postRequestComment,
} from '../api/spaClient';
import { AnalysisDetailSections, AnalysisSummaryBanner } from '../components/AnalysisDetailSections';
import { DeliveryRunPanel } from '../components/DeliveryRunPanel';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { MissionControl } from '../components/MissionControl';
import { useRequestCollaboration } from '../hooks/useRequestCollaboration';
import { formatApprovalAction, formatRequestStatus, getStatusNextStep } from '../utils/requestLabels';
import type {
  ApprovalHistoryItem,
  CommentSummary,
  EnhancementAnalysis,
  EnhancementRequestDetail,
} from '../types/spa';

interface RequestDetailAppProps {
  requestId: string;
}

export function RequestDetailApp({ requestId }: RequestDetailAppProps) {
  const [detail, setDetail] = useState<EnhancementRequestDetail | null>(null);
  const [analysis, setAnalysis] = useState<EnhancementAnalysis | null>(null);
  const [approvalHistory, setApprovalHistory] = useState<ApprovalHistoryItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [commentText, setCommentText] = useState('');
  const [commentInternal, setCommentInternal] = useState(false);
  const [postingComment, setPostingComment] = useState(false);

  const reload = useCallback(async () => {
    const [detailResult, analysisResult, historyResult] = await Promise.all([
      getRequestDetail(requestId),
      getRequestAnalysis(requestId),
      getApprovalHistory(requestId),
    ]);
    setDetail(detailResult);
    setAnalysis(analysisResult);
    setApprovalHistory(historyResult);
  }, [requestId]);

  const { presence, liveComments, analysisUpdateMessage } = useRequestCollaboration(requestId, {
    onAnalysisUpdated: () => {
      void reload().catch(() => undefined);
    },
  });

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);
      try {
        await reload();
      } catch {
        if (!cancelled) {
          setError('Failed to load request.');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, [reload]);

  useEffect(() => {
    if (!detail || (detail.status !== 'Analyzing' && detail.status !== 'AiAnalyzing' && detail.status !== 'Submitted')) {
      return;
    }

    const timer = window.setInterval(() => {
      void reload().catch(() => undefined);
    }, 5000);

    return () => window.clearInterval(timer);
  }, [detail?.status, reload]);

  const comments = useMemo(() => {
    const initial = detail?.comments ?? [];
    const merged = [...liveComments, ...initial];
    const seen = new Set<string>();
    return merged.filter((comment) => {
      if (seen.has(comment.id)) {
        return false;
      }

      seen.add(comment.id);
      return true;
    });
  }, [detail?.comments, liveComments]);

  async function onSubmitComment(event: FormEvent) {
    event.preventDefault();
    if (!commentText.trim()) {
      return;
    }

    setPostingComment(true);
    try {
      await postRequestComment(requestId, commentText.trim(), commentInternal);
      setCommentText('');
      await reload();
    } catch {
      setError('Failed to post comment.');
    } finally {
      setPostingComment(false);
    }
  }

  if (loading) {
    return (
      <div aria-busy="true" aria-live="polite">
        <p className="text-muted" role="status">
          Loading…
        </p>
        <LoadingSkeleton />
      </div>
    );
  }

  if (error || !detail) {
    return (
      <div className="alert alert-danger d-flex flex-wrap justify-content-between align-items-center gap-2" role="alert">
        <span>{error ?? 'Request not found.'}</span>
        <div className="d-flex gap-2">
          <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => void reload()}>
            Retry
          </button>
        </div>
      </div>
    );
  }

  const isAnalyzing =
    detail.status === 'Analyzing' || detail.status === 'AiAnalyzing' || detail.status === 'Submitted';

  return (
    <div aria-live="polite">
      <header className="page-header d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
        <div>
          <h1 className="h3 mb-1">{detail.title}</h1>
          <p className="mb-0">
            <span className="badge text-bg-secondary badge-status">{formatRequestStatus(detail.status)}</span>
            {detail.submittedByUserName ? (
              <span className="text-muted ms-2">Submitted by {detail.submittedByUserName}</span>
            ) : null}
          </p>
        </div>
        <div className="d-flex flex-wrap gap-2">
          <a href="/Spa/ApprovalQueue" className="btn btn-outline-primary btn-sm">
            Approval queue
          </a>
          {detail.targetApplicationId ? (
            <a
              href={`/Spa/SystemMap?applicationId=${detail.targetApplicationId}`}
              className="btn btn-outline-secondary btn-sm"
            >
              System map
            </a>
          ) : null}
        </div>
      </header>

      <div className="alert alert-light border mb-4" role="status">
        <strong>What happens next:</strong> {getStatusNextStep(detail.status)}
      </div>

      {analysisUpdateMessage ? (
        <div className="alert alert-info" role="status">
          {analysisUpdateMessage}
        </div>
      ) : null}

      {!analysis && isAnalyzing ? (
        <div className="card-panel p-4 mb-3" id="analysis-in-progress" role="status">
          <div className="d-flex align-items-center gap-2">
            <span className="spinner-border spinner-border-sm text-primary" aria-hidden="true" />
            <span>We are reviewing your request. This page refreshes automatically.</span>
          </div>
        </div>
      ) : null}

      <div className="row g-4">
        <div className="col-lg-8">
          <DeliveryRunPanel
            requestId={requestId}
            requestStatus={detail.status}
            desiredOutcome={detail.desiredOutcome}
          />

          <section className="card-panel p-4 mb-3">
            <h2 className="h6 text-muted text-uppercase">Your original request</h2>
            <p className="mb-2">
              <strong>What problem are you solving?</strong>
            </p>
            <p>{detail.businessDescription}</p>
            <p className="mb-2">
              <strong>What does success look like?</strong>
            </p>
            <p className="mb-0">{detail.desiredOutcome}</p>
            {detail.supportingNotes ? (
              <>
                <p className="mb-2 mt-3">
                  <strong>Additional notes</strong>
                </p>
                <p className="mb-0">{detail.supportingNotes}</p>
              </>
            ) : null}
          </section>

          {analysis ? (
            <>
              <AnalysisSummaryBanner analysis={analysis} />
              <MissionControl analysis={analysis} />
          {detail.targetApplicationId ? (
            <div className="mb-3">
              <a
                href={`/Spa/SystemMap?applicationId=${detail.targetApplicationId}`}
                className="btn btn-sm btn-outline-primary"
              >
                View affected systems
              </a>
            </div>
          ) : null}
              <AnalysisDetailSections analysis={analysis} />
            </>
          ) : null}
        </div>

        <div className="col-lg-4">
          <section className="card-panel p-3 mb-3" id="collaboration-panel" aria-label="Collaboration">
            <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
              <h2 className="h6 mb-0">Live collaboration</h2>
              <span className="small text-muted" id="collaboration-presence" aria-live="polite">
                {presence}
              </span>
            </div>

            <form onSubmit={(event) => void onSubmitComment(event)} className="mb-3">
              <label className="form-label" htmlFor="spa-comment-content">
                Add comment
              </label>
              <textarea
                id="spa-comment-content"
                className="form-control mb-2"
                rows={2}
                value={commentText}
                onChange={(event) => setCommentText(event.target.value)}
                required
              />
              <div className="form-check mb-2">
                <input
                  id="spa-comment-internal"
                  type="checkbox"
                  className="form-check-input"
                  checked={commentInternal}
                  onChange={(event) => setCommentInternal(event.target.checked)}
                />
                <label className="form-check-label" htmlFor="spa-comment-internal">
                  Internal note
                </label>
              </div>
              <button type="submit" className="btn btn-outline-primary btn-sm" disabled={postingComment}>
                {postingComment ? 'Posting…' : 'Post comment'}
              </button>
            </form>

            <div id="collaboration-live-comments" aria-live="polite" aria-relevant="additions">
              {comments.length === 0 ? (
                <p className="small text-muted mb-0">No comments yet.</p>
              ) : (
                comments.map((comment: CommentSummary) => (
                  <div key={comment.id} className="border-bottom py-2 collaboration-live-item">
                    <strong>{comment.userDisplayName}</strong>
                    {comment.isInternal ? (
                      <span className="badge text-bg-warning ms-1">Internal</span>
                    ) : null}
                    <p className="small mb-0">{comment.content}</p>
                  </div>
                ))
              )}
            </div>
          </section>

          <section className="card-panel p-3">
            <h2 className="h6 mb-3">Approval history</h2>
            {approvalHistory.length === 0 ? (
              <p className="text-muted small mb-0">No approval actions yet.</p>
            ) : (
              <ul className="list-group list-group-flush">
                {approvalHistory.map((action) => (
                  <li key={action.id} className="list-group-item px-0">
                    <strong>{formatApprovalAction(action.actionType)}</strong> by {action.userDisplayName}
                    <br />
                    <span className="text-muted small">
                      {new Date(action.createdAt).toLocaleString()}
                    </span>
                    {action.comments ? <p className="small mb-0 mt-1">{action.comments}</p> : null}
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>
      </div>
    </div>
  );
}

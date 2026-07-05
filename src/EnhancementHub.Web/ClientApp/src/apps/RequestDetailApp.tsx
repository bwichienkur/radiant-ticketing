import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import { getRequestAnalysis, getRequestDetail, postRequestComment } from '../api/spaClient';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { MissionControl, riskBadgeClass } from '../components/MissionControl';
import { useRequestCollaboration } from '../hooks/useRequestCollaboration';
import type { CommentSummary, EnhancementAnalysis, EnhancementRequestDetail } from '../types/spa';

interface RequestDetailAppProps {
  requestId: string;
}

export function RequestDetailApp({ requestId }: RequestDetailAppProps) {
  const [detail, setDetail] = useState<EnhancementRequestDetail | null>(null);
  const [analysis, setAnalysis] = useState<EnhancementAnalysis | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [commentText, setCommentText] = useState('');
  const [commentInternal, setCommentInternal] = useState(false);
  const [postingComment, setPostingComment] = useState(false);

  const reload = useCallback(async () => {
    const [detailResult, analysisResult] = await Promise.all([
      getRequestDetail(requestId),
      getRequestAnalysis(requestId),
    ]);
    setDetail(detailResult);
    setAnalysis(analysisResult);
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
    if (!detail || detail.status !== 'Analyzing') {
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
      <div className="alert alert-danger" role="alert">
        {error ?? 'Request not found.'}
      </div>
    );
  }

  return (
    <div aria-live="polite">
      <header className="mb-4">
        <h1 className="h3">{detail.title}</h1>
        <p>
          <span className="badge text-bg-secondary">{detail.status}</span>
          {detail.submittedByUserName ? (
            <span className="text-muted ms-2">{detail.submittedByUserName}</span>
          ) : null}
        </p>
      </header>

      {analysisUpdateMessage ? (
        <div className="alert alert-info" role="status">
          {analysisUpdateMessage}
        </div>
      ) : null}

      {detail.status === 'Analyzing' ? (
        <div className="alert alert-warning" role="status" id="analysis-in-progress">
          Analysis in progress… this page refreshes automatically.
        </div>
      ) : null}

      {analysis ? <MissionControl analysis={analysis} /> : null}

      <section className="card-panel p-4 mb-3">
        <h2 className="h6 text-muted text-uppercase">Business description</h2>
        <p>{detail.businessDescription}</p>
        <h2 className="h6 text-muted text-uppercase mt-3">Desired outcome</h2>
        <p className="mb-0">{detail.desiredOutcome}</p>
      </section>

      {analysis ? (
        <section className="card-panel p-4 analysis-summary-banner mb-3">
          <h2 className="h6">AI analysis (v{analysis.version})</h2>
          <p className="mb-2">{analysis.featureSummary ?? 'Analysis complete.'}</p>
          <span className={`badge ${riskBadgeClass(analysis.riskLevel)}`}>{analysis.riskLevel} risk</span>
        </section>
      ) : null}

      <section className="card-panel p-4 mb-3" id="collaboration-panel" aria-label="Collaboration">
        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <h2 className="h6 mb-0">Collaboration</h2>
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
          <button type="submit" className="btn btn-primary btn-sm" disabled={postingComment}>
            Post comment
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

      <div className="d-flex gap-2">
        <a href="/EnhancementRequests/Approve" className="btn btn-outline-primary btn-sm">
          Approval queue
        </a>
        <a href="/Spa/SystemMap" className="btn btn-outline-secondary btn-sm">
          System map (React)
        </a>
        <a href={`/EnhancementRequests/Details?id=${detail.id}`} className="btn btn-outline-secondary btn-sm">
          Classic view
        </a>
      </div>
    </div>
  );
}

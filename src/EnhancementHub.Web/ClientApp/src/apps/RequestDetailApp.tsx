import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import {
  getAnalysisEvolution,
  getApprovalHistory,
  getPlatformRuntimeStatus,
  getRequestAnalysis,
  getRequestDetail,
  postRequestComment,
} from '../api/spaClient';
import { AnalysisDetailSections, AnalysisSummaryBanner } from '../components/AnalysisDetailSections';
import { DeliveryRunPanel } from '../components/DeliveryRunPanel';
import { SpaLink } from '../components/SpaLink';
import {
  AlertBanner,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  SectionCard,
  StatusBadge,
  TabBar,
} from '../components/ui';
import { MissionControl } from '../components/MissionControl';
import { useRequestCollaboration } from '../hooks/useRequestCollaboration';
import { formatApprovalAction, getStatusNextStep } from '../utils/requestLabels';
import type {
  AnalysisComparison,
  ApprovalHistoryItem,
  CommentSummary,
  EnhancementAnalysis,
  EnhancementRequestDetail,
  PlatformRuntimeStatus,
} from '../types/spa';

interface RequestDetailAppProps {
  requestId: string;
}

type RequestDetailTab = 'overview' | 'analysis' | 'delivery' | 'activity';

export function RequestDetailApp({ requestId }: RequestDetailAppProps) {
  const [detail, setDetail] = useState<EnhancementRequestDetail | null>(null);
  const [analysis, setAnalysis] = useState<EnhancementAnalysis | null>(null);
  const [analysisEvolution, setAnalysisEvolution] = useState<AnalysisComparison | null>(null);
  const [approvalHistory, setApprovalHistory] = useState<ApprovalHistoryItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<RequestDetailTab>('overview');
  const [commentText, setCommentText] = useState('');
  const [commentInternal, setCommentInternal] = useState(false);
  const [postingComment, setPostingComment] = useState(false);
  const [runtimeStatus, setRuntimeStatus] = useState<PlatformRuntimeStatus | null>(null);

  const reload = useCallback(async () => {
    const [detailResult, analysisResult, historyResult] = await Promise.all([
      getRequestDetail(requestId),
      getRequestAnalysis(requestId),
      getApprovalHistory(requestId),
    ]);
    setDetail(detailResult);
    setAnalysis(analysisResult);
    setApprovalHistory(historyResult);

    if (analysisResult) {
      try {
        setAnalysisEvolution(await getAnalysisEvolution(requestId));
      } catch {
        setAnalysisEvolution(null);
      }
    } else {
      setAnalysisEvolution(null);
    }
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
    void getPlatformRuntimeStatus()
      .then(setRuntimeStatus)
      .catch(() => setRuntimeStatus(null));
  }, []);

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
    return <LoadingState label="Loading request…" />;
  }

  if (error || !detail) {
    return (
      <ErrorState
        message={error ?? 'Request not found.'}
        onRetry={() => void reload()}
      />
    );
  }

  const isAnalyzing =
    detail.status === 'Analyzing' || detail.status === 'AiAnalyzing' || detail.status === 'Submitted';

  const activityCount = comments.length + approvalHistory.length;

  return (
    <div className="eh-request-detail" aria-live="polite">
      {runtimeStatus && !runtimeStatus.aiConfigured && analysis ? (
        <AlertBanner variant="warning" title="Mock AI analysis" className="mb-3">
          AI provider is not configured ({runtimeStatus.aiProvider}). Analysis shown may be
          deterministic mock output — configure OpenAI or Azure OpenAI for production.
        </AlertBanner>
      ) : null}

      <PageHeader
        title={detail.title}
        titleAs="h1"
        description={
          <>
            <StatusBadge status={detail.status} />
            {detail.submittedByUserName ? (
              <span className="text-muted ms-2">Submitted by {detail.submittedByUserName}</span>
            ) : null}
          </>
        }
        actions={
          <>
            <SpaLink href="/Spa/ApprovalQueue" className="btn btn-outline-primary btn-sm">
              Approval queue
            </SpaLink>
            {detail.targetApplicationId ? (
              <SpaLink
                href={`/Spa/SystemMap?applicationId=${detail.targetApplicationId}`}
                className="btn btn-outline-secondary btn-sm"
              >
                System map
              </SpaLink>
            ) : null}
          </>
        }
      />

      <TabBar
        ariaLabel="Request sections"
        activeId={activeTab}
        onChange={(id) => setActiveTab(id as RequestDetailTab)}
        tabs={[
          { id: 'overview', label: 'Overview' },
          { id: 'analysis', label: 'Analysis', badge: analysis ? 'Ready' : isAnalyzing ? '…' : undefined },
          { id: 'delivery', label: 'Delivery' },
          { id: 'activity', label: 'Activity', badge: activityCount > 0 ? activityCount : undefined },
        ]}
      />

      {analysisUpdateMessage ? (
        <AlertBanner variant="info" className="mb-3">
          {analysisUpdateMessage}
        </AlertBanner>
      ) : null}

      {activeTab === 'overview' ? (
        <>
          <AlertBanner variant="neutral" title="What happens next:" className="mb-4">
            {getStatusNextStep(detail.status)}
          </AlertBanner>

          <SectionCard title="Your original request">
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
          </SectionCard>
        </>
      ) : null}

      {activeTab === 'analysis' ? (
        <>
          {!analysis && isAnalyzing ? (
            <div className="card-panel p-4 mb-3" id="analysis-in-progress" role="status">
              <div className="d-flex align-items-center gap-2">
                <span className="spinner-border spinner-border-sm text-primary" aria-hidden="true" />
                <span>We are reviewing your request. This page refreshes automatically.</span>
              </div>
            </div>
          ) : null}

          {analysis ? (
            <>
              <AnalysisSummaryBanner analysis={analysis} />
              <MissionControl analysis={analysis} />
              {detail.targetApplicationId ? (
                <div className="mb-3">
                  <SpaLink
                    href={`/Spa/SystemMap?applicationId=${detail.targetApplicationId}`}
                    className="btn btn-sm btn-outline-primary"
                  >
                    View affected systems
                  </SpaLink>
                </div>
              ) : null}
              <AnalysisDetailSections analysis={analysis} />
              {analysisEvolution &&
              analysisEvolution.fieldChanges.some((change) => change.changed) ? (
                <SectionCard title="AI recommendation vs architect edits" className="mb-3">
                  <p className="small text-muted">
                    Comparing AI baseline (v{analysisEvolution.versionA}) to current state
                    {analysisEvolution.architectEditsBetweenVersions > 0
                      ? ` · ${analysisEvolution.architectEditsBetweenVersions} architect edit(s) recorded`
                      : ''}
                    .
                  </p>
                  <div className="table-responsive">
                    <table className="table table-sm align-middle mb-0">
                      <thead>
                        <tr>
                          <th scope="col">Field</th>
                          <th scope="col">AI recommendation</th>
                          <th scope="col">Current</th>
                        </tr>
                      </thead>
                      <tbody>
                        {analysisEvolution.fieldChanges
                          .filter((change) => change.changed)
                          .map((change) => (
                            <tr key={change.fieldName}>
                              <td className="fw-semibold">{change.fieldName}</td>
                              <td className="small">{change.valueA ?? '—'}</td>
                              <td className="small">{change.valueB ?? '—'}</td>
                            </tr>
                          ))}
                      </tbody>
                    </table>
                  </div>
                </SectionCard>
              ) : null}
            </>
          ) : !isAnalyzing ? (
            <SectionCard title="Analysis">
              <p className="text-muted mb-0">No analysis is available for this request yet.</p>
            </SectionCard>
          ) : null}
        </>
      ) : null}

      {activeTab === 'delivery' ? (
        <DeliveryRunPanel
          requestId={requestId}
          requestStatus={detail.status}
          desiredOutcome={detail.desiredOutcome}
        />
      ) : null}

      {activeTab === 'activity' ? (
        <div className="row g-4">
          <div className="col-lg-7">
            <SectionCard title="Live collaboration" id="collaboration-panel" ariaLabel="Collaboration">
              <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
                <span className="small text-muted" id="collaboration-presence" aria-live="polite">
                  {presence}
                </span>
              </div>

              <form onSubmit={(event) => void onSubmitComment(event)} className="mb-3">
                <FormField id="spa-comment-content" label="Add comment" required>
                  <textarea
                    id="spa-comment-content"
                    className="form-control"
                    rows={2}
                    value={commentText}
                    onChange={(event) => setCommentText(event.target.value)}
                    required
                  />
                </FormField>
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
            </SectionCard>
          </div>

          <div className="col-lg-5">
            <SectionCard title="Approval history">
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
            </SectionCard>
          </div>
        </div>
      ) : null}
    </div>
  );
}

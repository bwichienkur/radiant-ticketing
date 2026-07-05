import { useEffect, useState } from 'react';
import { getRequestAnalysis, getRequestDetail } from '../api/spaClient';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { MissionControl, riskBadgeClass } from '../components/MissionControl';
import type { EnhancementAnalysis, EnhancementRequestDetail } from '../types/spa';

interface RequestDetailAppProps {
  requestId: string;
}

export function RequestDetailApp({ requestId }: RequestDetailAppProps) {
  const [detail, setDetail] = useState<EnhancementRequestDetail | null>(null);
  const [analysis, setAnalysis] = useState<EnhancementAnalysis | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);
      try {
        const [detailResult, analysisResult] = await Promise.all([
          getRequestDetail(requestId),
          getRequestAnalysis(requestId),
        ]);
        if (!cancelled) {
          setDetail(detailResult);
          setAnalysis(analysisResult);
        }
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
  }, [requestId]);

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

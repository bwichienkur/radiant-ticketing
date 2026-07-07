import { useEffect, useState } from 'react';
import { getPortfolioHealth, getPortfolioHealthExportUrl } from '../api/spaClient';
import { ErrorState, EmptyState, LoadingState, PageHeader } from '../components/ui';
import { SpaLink } from '../components/SpaLink';
import type { PortfolioHealthReport } from '../types/spa';

function riskClass(score: number): string {
  if (score >= 70) {
    return 'portfolio-heat-critical';
  }

  if (score >= 40) {
    return 'portfolio-heat-warning';
  }

  return 'portfolio-heat-healthy';
}

export function PortfolioHealthApp() {
  const [report, setReport] = useState<PortfolioHealthReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setReport(await getPortfolioHealth());
    } catch {
      setError('Unable to load portfolio health.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  if (loading) {
    return <LoadingState label="Loading portfolio health…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void load()} />;
  }

  if (!report) {
    return <ErrorState message="No portfolio health data." onRetry={() => void load()} />;
  }

  return (
    <div>
      <PageHeader
        title="Portfolio health"
        description="Risk heatmap across applications — drift, pending approvals, and stale indexing"
        actions={
          <a href={getPortfolioHealthExportUrl()} className="btn btn-outline-primary btn-sm">
            Export CSV
          </a>
        }
      />

      <div className="card-panel p-3 mb-3">
        <div className="small text-muted mb-2">
          Higher scores indicate more unresolved drift, pending high-risk approvals, or stale repositories.
        </div>
        <div className="portfolio-heatmap" role="table" aria-label="Application risk heatmap">
          <div className="portfolio-heatmap-header" role="row">
            <span role="columnheader">Application</span>
            <span role="columnheader">Drift</span>
            <span role="columnheader">Pending</span>
            <span role="columnheader">High risk</span>
            <span role="columnheader">Stale repos</span>
            <span role="columnheader">Score</span>
          </div>
          {report.applications.length === 0 ? (
            <EmptyState
              title="No applications indexed"
              description="Register applications and connect repositories to populate the portfolio risk heatmap."
              icon="inbox"
            />
          ) : (
            report.applications.map((row) => (
              <div key={row.applicationId} className={`portfolio-heatmap-row ${riskClass(row.riskScore)}`} role="row">
                <span role="cell">
                  <SpaLink href={`/Spa/SystemMap?ApplicationId=${row.applicationId}`}>{row.applicationName}</SpaLink>
                </span>
                <span role="cell">{row.unresolvedDriftCount}</span>
                <span role="cell">{row.pendingRequestCount}</span>
                <span role="cell">{row.highRiskPendingCount}</span>
                <span role="cell">{row.staleRepositoryCount}</span>
                <span role="cell">
                  <strong>{row.riskScore}</strong>
                </span>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}

import { useEffect, useState } from 'react';
import { exportRoiCsv, getRoiReport } from '../api/spaClient';
import { readSpaContext } from '../spaRoutes';
import { ErrorState, LoadingState, PageHeader } from '../components/ui';
import type { RoiReport } from '../types/spa';

function formatHours(value?: number | null): string {
  if (value === null || value === undefined) {
    return '—';
  }

  return `${value.toFixed(2)} h`;
}

function formatNps(value?: number | null): string {
  if (value === null || value === undefined) {
    return '—';
  }

  return value.toFixed(1);
}

export function InsightsApp() {
  const { isAdmin, isApprover } = readSpaContext();
  const [report, setReport] = useState<RoiReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);

  async function loadReport() {
    setLoading(true);
    setError(null);
    try {
      const data = await getRoiReport();
      setReport(data);
    } catch {
      setError('Unable to load ROI report.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadReport();
  }, []);

  async function handleExport() {
    setExporting(true);
    try {
      await exportRoiCsv();
    } catch {
      setError('CSV export failed. Try again or contact an administrator.');
    } finally {
      setExporting(false);
    }
  }

  if (!isAdmin && !isApprover) {
    return <ErrorState message="Administrator or approver access is required to view insights." />;
  }

  if (loading) {
    return <LoadingState label="Loading ROI insights…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void loadReport()} />;
  }

  if (!report) {
    return <ErrorState message="Unable to load ROI report." onRetry={() => void loadReport()} />;
  }

  return (
    <div>
      <PageHeader
        title="ROI Dashboard"
        description="Analysis time saved, risk governance, and drift resolution metrics"
        actions={
          <button
            type="button"
            className="btn btn-outline-primary btn-sm"
            onClick={() => void handleExport()}
            disabled={exporting}
          >
            {exporting ? 'Exporting…' : 'Export CSV'}
          </button>
        }
      />

      <div className="row g-3 mb-4">
        <div className="col-md-3">
          <div className="stat-card">
            <div className="label">Analyses completed</div>
            <div className="value">{report.totalAnalysesCompleted}</div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="stat-card">
            <div className="label">Avg analysis time</div>
            <div className="value fs-5">{report.averageAnalysisDurationMinutes} min</div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="stat-card">
            <div className="label">Est. hours saved</div>
            <div className="value fs-5">{report.estimatedHoursSaved}</div>
            <div className="small text-muted">
              vs {report.estimatedManualAnalysisHoursPerRequest} h manual baseline
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="stat-card">
            <div className="label">High/critical approved</div>
            <div className="value">{report.highOrCriticalRiskApprovedCount}</div>
          </div>
        </div>
      </div>

      <div className="row g-3 mb-4">
        <div className="col-md-4">
          <div className="stat-card">
            <div className="label">Drift findings resolved</div>
            <div className="value">
              {report.driftFindingsResolved} / {report.driftFindingsTotal}
            </div>
          </div>
        </div>
        <div className="col-md-4">
          <div className="stat-card">
            <div className="label">Architect edits</div>
            <div className="value">{report.architectEditsRecorded}</div>
          </div>
        </div>
        <div className="col-md-4">
          <div className="stat-card">
            <div className="label">Human-approved findings</div>
            <div className="value">
              {report.humanApprovedFindings} / {report.aiSuggestedFindings} AI suggested
            </div>
          </div>
        </div>
      </div>

      <div className="row g-3 mb-4">
        <div className="col-md-3">
          <div className="stat-card">
            <div className="label">Avg time to analysis</div>
            <div className="value fs-5">{formatHours(report.averageTimeToAnalysisHours)}</div>
            <div className="small text-muted">Request submitted → first analysis</div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="stat-card">
            <div className="label">Avg time to approval</div>
            <div className="value fs-5">{formatHours(report.averageTimeToApprovalHours)}</div>
            <div className="small text-muted">Request submitted → approved</div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="stat-card">
            <div className="label">Mock AI runs</div>
            <div className="value fs-5">{report.mockAiRunPercent}%</div>
            <div className="small text-muted">{report.totalAiRunsCompleted} completed run(s)</div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="stat-card">
            <div className="label">Pilot NPS</div>
            <div className="value fs-5">{formatNps(report.averagePilotNps)}</div>
            <div className="small text-muted">{report.totalFeedbackSubmissions} in-product submission(s)</div>
          </div>
        </div>
      </div>

      {report.templateUsageByCategory.length > 0 ? (
        <div className="card-panel p-4">
          <h2 className="h6">Template usage by domain</h2>
          <ul className="mb-0">
            {report.templateUsageByCategory.map((item) => (
              <li key={item.category}>
                <strong>{item.category}:</strong> {item.requestCount} request(s)
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
}

import { riskBadgeClass } from './MissionControl';
import type { EnhancementAnalysis } from '../types/spa';

interface AnalysisDetailSectionsProps {
  analysis: EnhancementAnalysis;
}

export function AnalysisSummaryBanner({ analysis }: AnalysisDetailSectionsProps) {
  return (
    <section className="card-panel p-4 mb-3 analysis-summary-banner">
      <div className="d-flex justify-content-between align-items-start flex-wrap gap-3">
        <div className="flex-grow-1">
          <div className="d-flex align-items-center flex-wrap gap-2 mb-2">
            <h2 className="h5 mb-0">AI Analysis</h2>
            <span className={`badge ${riskBadgeClass(analysis.riskLevel)} badge-status`}>
              {analysis.riskLevel} risk
            </span>
            {analysis.needsClarification ? (
              <span className="badge text-bg-secondary badge-status">Clarification needed</span>
            ) : null}
          </div>
          <p className="mb-0">{analysis.featureSummary ?? 'Analysis complete — see details below.'}</p>
        </div>
        <div className="d-flex gap-3">
          <div className="text-center">
            <div className="small text-muted">Confidence</div>
            <div className="fw-semibold fs-5">{Math.round((analysis.confidenceScore ?? 0) * 100)}%</div>
          </div>
          <div className="text-center">
            <div className="small text-muted">Version</div>
            <div className="fw-semibold fs-5">{analysis.version}</div>
          </div>
        </div>
      </div>
      {analysis.riskExplanation ? (
        <p className="text-muted small mb-0 mt-3">
          <strong>Risk:</strong> {analysis.riskExplanation}
        </p>
      ) : null}
    </section>
  );
}

export function AnalysisDetailSections({ analysis }: AnalysisDetailSectionsProps) {
  const affectedApps = analysis.affectedApplications ?? [];
  const dbChanges = analysis.databaseChangeRecommendations ?? [];
  const apiChanges = analysis.apiChangeRecommendations ?? [];

  return (
    <section className="card-panel p-4 mb-3">
      {analysis.technicalRequirements ? (
        <div className="detail-section mb-3">
          <h3 className="h6">Technical requirements</h3>
          <pre className="mb-0 small">{analysis.technicalRequirements}</pre>
        </div>
      ) : null}

      {analysis.testingPlan ? (
        <div className="detail-section mb-3">
          <h3 className="h6">Testing plan</h3>
          <p className="mb-0">{analysis.testingPlan}</p>
        </div>
      ) : null}

      {affectedApps.length > 0 ? (
        <div className="detail-section mb-3">
          <h3 className="h6">Affected applications</h3>
          <ul className="mb-0">
            {affectedApps.map((app) => (
              <li key={app.applicationId}>
                {app.applicationName ?? app.applicationId}
                {app.impactDescription ? ` — ${app.impactDescription}` : ''}
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {dbChanges.length > 0 ? (
        <div className="detail-section mb-3">
          <h3 className="h6">Database changes</h3>
          <div className="table-responsive">
            <table className="table table-sm mb-0">
              <thead>
                <tr>
                  <th scope="col">Table</th>
                  <th scope="col">Type</th>
                  <th scope="col">Description</th>
                </tr>
              </thead>
              <tbody>
                {dbChanges.map((change) => (
                  <tr key={`${change.tableName}-${change.changeType}`}>
                    <td>{change.tableName}</td>
                    <td>{change.changeType}</td>
                    <td>{change.description}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}

      {apiChanges.length > 0 ? (
        <div className="detail-section">
          <h3 className="h6">API changes</h3>
          <div className="table-responsive">
            <table className="table table-sm mb-0">
              <thead>
                <tr>
                  <th scope="col">Endpoint</th>
                  <th scope="col">Type</th>
                  <th scope="col">Description</th>
                </tr>
              </thead>
              <tbody>
                {apiChanges.map((change) => (
                  <tr key={`${change.endpoint}-${change.changeType}`}>
                    <td>{change.endpoint}</td>
                    <td>{change.changeType}</td>
                    <td>{change.description}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </section>
  );
}

import { useState } from 'react';
import { formatConfidenceLabel } from '../utils/requestLabels';
import { riskBadgeClass, riskPlainLabel } from '../utils/riskLabels';
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
            <h2 className="h5 mb-0">What we found</h2>
            <span className={`badge ${riskBadgeClass(analysis.riskLevel)} badge-status`}>
              {riskPlainLabel(analysis.riskLevel)}
            </span>
            {analysis.needsClarification ? (
              <span className="badge text-bg-secondary badge-status">More detail needed</span>
            ) : null}
          </div>
          <p className="mb-0">{analysis.featureSummary ?? 'Review complete — see details below.'}</p>
        </div>
        <div className="text-center" title="How confident the AI is in this assessment">
          <div className="small text-muted">AI confidence</div>
          <div className="fw-semibold fs-5">{formatConfidenceLabel(analysis.confidenceScore ?? 0)}</div>
        </div>
      </div>
      {analysis.riskExplanation ? (
        <p className="text-muted small mb-0 mt-3">
          <strong>Why this matters:</strong> {analysis.riskExplanation}
        </p>
      ) : null}
    </section>
  );
}

export function AnalysisDetailSections({ analysis }: AnalysisDetailSectionsProps) {
  const [showTechnical, setShowTechnical] = useState(false);
  const affectedApps = analysis.affectedApplications ?? [];
  const dbChanges = analysis.databaseChangeRecommendations ?? [];
  const apiChanges = analysis.apiChangeRecommendations ?? [];

  const hasTechnicalContent =
    Boolean(analysis.technicalRequirements)
    || Boolean(analysis.testingPlan)
    || affectedApps.length > 0
    || dbChanges.length > 0
    || apiChanges.length > 0;

  if (!hasTechnicalContent) {
    return null;
  }

  return (
    <section className="card-panel p-4 mb-3">
      <div className="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3">
        <h2 className="h6 mb-0">Technical details for your IT team</h2>
        <button
          type="button"
          className="btn btn-sm btn-outline-secondary"
          aria-expanded={showTechnical}
          onClick={() => setShowTechnical((prev) => !prev)}
        >
          {showTechnical ? 'Hide details' : 'Show details'}
        </button>
      </div>

      {!showTechnical ? (
        <p className="small text-muted mb-0">
          Database, API, and implementation notes are available for technical reviewers.
        </p>
      ) : (
        <>
          {analysis.technicalRequirements ? (
            <div className="detail-section mb-3">
              <h3 className="h6">Implementation notes</h3>
              <pre className="mb-0 small">{analysis.technicalRequirements}</pre>
            </div>
          ) : null}

          {analysis.testingPlan ? (
            <div className="detail-section mb-3">
              <h3 className="h6">Suggested testing</h3>
              <p className="mb-0">{analysis.testingPlan}</p>
            </div>
          ) : null}

          {affectedApps.length > 0 ? (
            <div className="detail-section mb-3">
              <h3 className="h6">Systems that may change</h3>
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
        </>
      )}
    </section>
  );
}

import type { EnhancementAnalysis } from '../types/spa';
import { formatConfidenceLabel } from '../utils/requestLabels';
import { riskBadgeClass, riskPlainLabel } from '../utils/riskLabels';

interface MissionControlProps {
  analysis: EnhancementAnalysis;
}

export function MissionControl({ analysis }: MissionControlProps) {
  const affected = analysis.affectedApplications?.length ?? 0;
  const dbChanges = analysis.databaseChangeRecommendations?.length ?? 0;
  const apiChanges = analysis.apiChangeRecommendations?.length ?? 0;

  return (
    <section className="card-panel p-4 mb-3">
      <h2 className="eh-section-title mb-3">Impact at a glance</h2>
      <div className="mission-control-grid">
        <div className="stat-card" title="How much this change could affect your systems">
          <div className="label">Impact level</div>
          <div className="value fs-5">
            <span className={`badge ${riskBadgeClass(analysis.riskLevel)}`}>
              {riskPlainLabel(analysis.riskLevel)}
            </span>
          </div>
        </div>
        <div
          className="stat-card"
          title="How confident the AI is in this assessment — higher is better"
        >
          <div className="label">AI confidence</div>
          <div className="value fs-5">{formatConfidenceLabel(analysis.confidenceScore ?? 0)}</div>
        </div>
        <div className="stat-card" title="Applications that may need changes">
          <div className="label">Systems affected</div>
          <div className="value fs-5">{affected}</div>
        </div>
        <div
          className="stat-card"
          title="Suggested database and API changes for your IT team"
        >
          <div className="label">Technical changes</div>
          <div className="value fs-5">
            {dbChanges} database · {apiChanges} API
          </div>
        </div>
      </div>
    </section>
  );
}

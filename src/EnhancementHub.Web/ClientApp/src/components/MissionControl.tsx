import type { EnhancementAnalysis } from '../types/spa';

function riskBadgeClass(risk: string): string {
  switch (risk) {
    case 'Critical':
      return 'text-bg-danger';
    case 'High':
      return 'text-bg-warning';
    case 'Medium':
      return 'text-bg-info';
    default:
      return 'text-bg-success';
  }
}

interface MissionControlProps {
  analysis: EnhancementAnalysis;
}

export function MissionControl({ analysis }: MissionControlProps) {
  const affected = analysis.affectedApplications?.length ?? 0;
  const dbChanges = analysis.databaseChangeRecommendations?.length ?? 0;
  const apiChanges = analysis.apiChangeRecommendations?.length ?? 0;

  return (
    <section className="card-panel p-4 mb-3">
      <h2 className="h6 mb-3">Mission control</h2>
      <div className="mission-control-grid">
        <div className="stat-card">
          <div className="label">Risk</div>
          <div className="value fs-5">
            <span className={`badge ${riskBadgeClass(analysis.riskLevel)}`}>{analysis.riskLevel}</span>
          </div>
        </div>
        <div className="stat-card">
          <div className="label">Confidence</div>
          <div className="value fs-5">{Math.round((analysis.confidenceScore ?? 0) * 100)}%</div>
        </div>
        <div className="stat-card">
          <div className="label">Affected apps</div>
          <div className="value fs-5">{affected}</div>
        </div>
        <div className="stat-card">
          <div className="label">DB / API changes</div>
          <div className="value fs-5">
            {dbChanges} / {apiChanges}
          </div>
        </div>
      </div>
    </section>
  );
}

export { riskBadgeClass };

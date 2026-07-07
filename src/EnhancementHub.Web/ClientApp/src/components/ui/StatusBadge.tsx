import { formatRequestStatus, normalizeRequestStatus } from '../../utils/requestLabels';
import { normalizeRiskLevel, riskBadgeClass } from '../../utils/riskLabels';

function statusBadgeClass(status: string | number): string {
  const normalized = normalizeRequestStatus(status).toLowerCase();
  if (normalized.includes('approv')) {
    return 'eh-badge-status-approved';
  }
  if (normalized.includes('reject') || normalized.includes('cancel')) {
    return 'eh-badge-status-rejected';
  }
  if (normalized.includes('pending') || normalized.includes('analyz') || normalized.includes('submitted')) {
    return 'eh-badge-status-pending';
  }
  return 'eh-badge-risk-medium';
}

interface StatusBadgeProps {
  status?: string | number;
  risk?: string | number;
}

export function StatusBadge({ status, risk }: StatusBadgeProps) {
  if (risk !== undefined && risk !== null) {
    const normalized = normalizeRiskLevel(risk);
    if (!normalized) {
      return <span className="text-muted">—</span>;
    }

    return (
      <span className={`eh-status-chip badge ${riskBadgeClass(normalized)} badge-status`}>{normalized}</span>
    );
  }

  if (status === undefined || status === null) {
    return null;
  }

  return (
    <span className={`eh-status-chip badge badge-status ${statusBadgeClass(status)}`}>
      {formatRequestStatus(status)}
    </span>
  );
}

import { formatRequestStatus } from '../../utils/requestLabels';
import { normalizeRiskLevel, riskBadgeClass } from '../../utils/riskLabels';

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
      <span className={`badge ${riskBadgeClass(normalized)} badge-status`}>{normalized}</span>
    );
  }

  if (status === undefined || status === null) {
    return null;
  }

  return (
    <span className="badge text-bg-secondary badge-status">{formatRequestStatus(status)}</span>
  );
}

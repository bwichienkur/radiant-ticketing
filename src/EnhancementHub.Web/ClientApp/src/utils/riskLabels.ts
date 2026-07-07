const RISK_BY_VALUE: Record<number, string> = {
  0: 'Low',
  1: 'Medium',
  2: 'High',
  3: 'Critical',
};

export function normalizeRiskLevel(risk: string | number | undefined | null): string | undefined {
  if (risk === undefined || risk === null) {
    return undefined;
  }

  if (typeof risk === 'number') {
    return RISK_BY_VALUE[risk] ?? String(risk);
  }

  if (/^\d+$/.test(risk)) {
    return RISK_BY_VALUE[Number(risk)] ?? risk;
  }

  return risk;
}

export function riskBadgeClass(risk: string): string {
  switch (risk) {
    case 'Critical':
      return 'eh-badge-risk-critical';
    case 'High':
      return 'eh-badge-risk-high';
    case 'Medium':
      return 'eh-badge-risk-medium';
    default:
      return 'eh-badge-risk-low';
  }
}

export function riskPlainLabel(risk: string): string {
  switch (risk) {
    case 'Critical':
      return 'Very high impact';
    case 'High':
      return 'High impact';
    case 'Medium':
      return 'Moderate impact';
    default:
      return 'Low impact';
  }
}

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
      return 'text-bg-danger';
    case 'High':
      return 'text-bg-warning';
    case 'Medium':
      return 'text-bg-info';
    default:
      return 'text-bg-success';
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

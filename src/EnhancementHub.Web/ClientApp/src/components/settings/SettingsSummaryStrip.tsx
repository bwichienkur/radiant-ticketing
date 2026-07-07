import { useSettingsWorkspaceSummary } from '../../hooks/useSettingsWorkspaceSummary';

function SummaryCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: string | number;
  tone?: 'healthy' | 'warning' | 'critical';
}) {
  return (
    <div className={`eh-settings-summary-card ${tone ? `eh-settings-summary-card--${tone}` : ''}`.trim()}>
      <div className="eh-settings-summary-card__label">{label}</div>
      <div className="eh-settings-summary-card__value">{value}</div>
    </div>
  );
}

export function SettingsSummaryStrip() {
  const summary = useSettingsWorkspaceSummary();

  if (summary.loading) {
    return (
      <div className="eh-settings-summary-strip" aria-busy="true">
        {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="eh-settings-summary-card eh-settings-summary-card--skeleton">
            <div className="eh-skeleton-line w-50" />
            <div className="eh-skeleton-line w-75 h-lg" />
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className="eh-settings-summary-strip">
      <SummaryCard label="Workspace" value={summary.workspaceName} />
      <SummaryCard label="Current plan" value={summary.plan} />
      <SummaryCard label="Users" value={summary.userCount} />
      <SummaryCard label="API keys" value={summary.apiKeyCount} />
      <SummaryCard label="API usage" value={summary.apiUsageLabel} />
      <SummaryCard label="Storage" value={summary.storageLabel} />
      <SummaryCard label="Status" value={summary.statusLabel} tone={summary.statusTone} />
    </div>
  );
}

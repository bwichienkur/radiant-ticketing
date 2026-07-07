import { useCallback, useEffect, useState } from 'react';
import {
  getAdminTenancy,
  getAuthenticationConfigurationStatus,
  getUserAppearance,
  listAdminTeams,
  listServiceApiKeys,
} from '../api/spaClient';
import type { TeamSummary, ServiceApiKeySummary } from '../types/spa';

export interface SettingsWorkspaceSummary {
  workspaceName: string;
  plan: string;
  userCount: number;
  apiKeyCount: number;
  apiUsageLabel: string;
  storageLabel: string;
  statusLabel: string;
  statusTone: 'healthy' | 'warning' | 'critical';
  loading: boolean;
}

function formatBytes(bytes: number): string {
  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(0)} KB`;
  }
  if (bytes < 1024 * 1024 * 1024) {
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
}

export function useSettingsWorkspaceSummary(): SettingsWorkspaceSummary {
  const [summary, setSummary] = useState<SettingsWorkspaceSummary>({
    workspaceName: 'Workspace',
    plan: '—',
    userCount: 0,
    apiKeyCount: 0,
    apiUsageLabel: '—',
    storageLabel: '—',
    statusLabel: 'Loading',
    statusTone: 'healthy',
    loading: true,
  });

  const load = useCallback(async () => {
    try {
      const [appearance, tenancy, teams, apiKeys, auth] = await Promise.all([
        getUserAppearance().catch(() => null),
        getAdminTenancy().catch(() => null),
        listAdminTeams().catch(() => []),
        listServiceApiKeys().catch(() => []),
        getAuthenticationConfigurationStatus().catch(() => null),
      ]);

      const billing = tenancy?.billing;
      const memberCount = teams.reduce((sum: number, t: TeamSummary) => sum + (t.memberCount ?? 0), 0);
      const workspaceName =
        billing?.tenantName ?? appearance?.branding?.productName ?? 'Default workspace';
      const plan = billing?.plan ?? (billing?.isTrialActive ? 'Trial' : 'Starter');
      const analyses = billing
        ? `${billing.analysisCountThisMonth}/${billing.maxAnalysesPerMonth} analyses`
        : '—';
      const storage = billing
        ? `${formatBytes(billing.storageBytes)} / ${billing.maxStorageMegabytes} MB`
        : '—';

      let statusLabel = 'Operational';
      let statusTone: SettingsWorkspaceSummary['statusTone'] = 'healthy';
      if (billing?.isOverLimit) {
        statusLabel = 'Over limit';
        statusTone = 'critical';
      } else if (billing?.isTrialExpired) {
        statusLabel = 'Trial expired';
        statusTone = 'critical';
      } else if (auth && !auth.isProductionReady && auth.openIdConnectEnabled) {
        statusLabel = 'Needs attention';
        statusTone = 'warning';
      } else if (billing?.isTrialActive) {
        statusLabel = 'Trial active';
        statusTone = 'warning';
      }

      setSummary({
        workspaceName,
        plan,
        userCount: memberCount || teams.length,
        apiKeyCount: apiKeys.filter((k: ServiceApiKeySummary) => k.isActive).length,
        apiUsageLabel: analyses,
        storageLabel: storage,
        statusLabel,
        statusTone,
        loading: false,
      });
    } catch {
      setSummary((prev) => ({ ...prev, loading: false, statusLabel: 'Unavailable', statusTone: 'warning' }));
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return summary;
}

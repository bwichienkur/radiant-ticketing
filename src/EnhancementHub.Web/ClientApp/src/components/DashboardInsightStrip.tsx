import { SpaLink } from './SpaLink';
import type { DashboardPageData } from '../types/spa';

interface InsightItem {
  message: string;
  href?: string;
  actionLabel?: string;
  tone: 'neutral' | 'warning' | 'urgent' | 'success';
}

function buildInsights(data: DashboardPageData): InsightItem[] {
  const { report, insights, isApprover, showOnboardingChecklist } = data;
  const items: InsightItem[] = [];

  if (isApprover && insights.myPendingApprovals > 0) {
    items.push({
      message: `${insights.myPendingApprovals} request${insights.myPendingApprovals === 1 ? '' : 's'} need your approval`,
      href: '/Spa/ApprovalQueue',
      actionLabel: 'Review queue',
      tone: 'urgent',
    });
  }

  if (report.highRiskPendingApprovalCount > 0) {
    items.push({
      message: `${report.highRiskPendingApprovalCount} high-risk request${report.highRiskPendingApprovalCount === 1 ? '' : 's'} awaiting decision`,
      href: '/Spa/ApprovalQueue',
      actionLabel: 'View high risk',
      tone: 'warning',
    });
  }

  if (insights.unresolvedDriftFindings > 0) {
    items.push({
      message: `${insights.unresolvedDriftFindings} unresolved schema drift finding${insights.unresolvedDriftFindings === 1 ? '' : 's'}`,
      href: '/Spa/SchemaDrift',
      actionLabel: 'Review drift',
      tone: 'warning',
    });
  }

  if (insights.myAwaitingAnalysis > 0) {
    items.push({
      message: `${insights.myAwaitingAnalysis} of your requests are awaiting analysis`,
      href: '/Spa/RequestList?status=AiAnalyzing',
      actionLabel: 'Track progress',
      tone: 'neutral',
    });
  }

  if (insights.staleRepositoryCount > 0) {
    items.push({
      message: `${insights.staleRepositoryCount} repositor${insights.staleRepositoryCount === 1 ? 'y is' : 'ies are'} stale — re-index to refresh portfolio intelligence`,
      href: '/Spa/Repositories',
      actionLabel: 'Open repositories',
      tone: 'warning',
    });
  }

  if (showOnboardingChecklist && report.totalRequests === 0) {
    items.push({
      message: 'Connect your first application to unlock portfolio intelligence and faster intake',
      href: '/Spa/OnboardingWizard',
      actionLabel: 'Start setup',
      tone: 'neutral',
    });
  }

  if (items.length === 0 && report.totalRequests > 0) {
    items.push({
      message: 'You are caught up — no urgent items need attention right now',
      tone: 'success',
    });
  }

  return items.slice(0, 3);
}

interface DashboardInsightStripProps {
  data: DashboardPageData;
}

export function DashboardInsightStrip({ data }: DashboardInsightStripProps) {
  const items = buildInsights(data);
  if (items.length === 0) {
    return null;
  }

  const primary = items[0];

  return (
    <section
      className={`eh-insight-strip eh-insight-strip--${primary.tone} mb-4`}
      aria-label="Priority insights"
      data-tour="dashboard-insights"
    >
      <div className="eh-insight-strip-main">
        <span className="eh-insight-strip-label">Today</span>
        <p className="eh-insight-strip-message mb-0">{primary.message}</p>
      </div>
      {primary.href && primary.actionLabel ? (
        <SpaLink href={primary.href} className="btn btn-sm btn-outline-primary eh-insight-strip-action">
          {primary.actionLabel}
        </SpaLink>
      ) : null}
      {items.length > 1 ? (
        <ul className="eh-insight-strip-more list-unstyled mb-0">
          {items.slice(1).map((item) => (
            <li key={item.message}>
              {item.href ? (
                <SpaLink href={item.href} className="eh-insight-strip-more-link">
                  {item.message}
                </SpaLink>
              ) : (
                <span>{item.message}</span>
              )}
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}

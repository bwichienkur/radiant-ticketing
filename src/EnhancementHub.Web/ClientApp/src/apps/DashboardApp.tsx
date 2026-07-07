import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { getDashboard } from '../api/spaClient';
import { DashboardInsightStrip } from '../components/DashboardInsightStrip';
import { SpaLink } from '../components/SpaLink';
import {
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
} from '../components/ui';
import type { DashboardPageData, DashboardActivityItem } from '../types/spa';

export function DashboardApp() {
  const [data, setData] = useState<DashboardPageData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function loadDashboard() {
    setLoading(true);
    setError(null);
    try {
      const dashboard = await getDashboard();
      setData(dashboard);
    } catch {
      setError('Failed to load dashboard.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadDashboard();
  }, []);

  useEffect(() => {
    if (!loading && data) {
      window.setTimeout(() => {
        const ehUx = (window as Window & { EhUx?: { initProductTour?: () => void } }).EhUx;
        ehUx?.initProductTour?.();
      }, 100);
    }
  }, [loading, data]);

  const maxTrend = useMemo(() => {
    const peak = data?.insights.requestsLast7Days.reduce((max, day) => Math.max(max, day.count), 0) ?? 0;
    return peak > 0 ? peak : 1;
  }, [data]);

  if (loading) {
    return <LoadingState label="Loading dashboard…" />;
  }

  if (error || !data) {
    return (
      <ErrorState
        message={error ?? 'Dashboard unavailable.'}
        onRetry={() => void loadDashboard()}
      />
    );
  }

  const { report, insights, onboardingStatus, isApprover, showOnboardingChecklist } = data;
  const hasRequests = report.totalRequests > 0;

  return (
    <div className="eh-dashboard" aria-live="polite">
      <PageHeader
        title="Dashboard"
        description="Track your change requests and see what needs attention"
        tourId="dashboard-header"
        actions={
          <>
            <SpaLink href="/Spa/OnboardingWizard" className="btn btn-outline-primary">
              Set up a system
            </SpaLink>
            <SpaLink href="/Spa/CreateRequest" className="btn btn-primary" data-tour="new-request">
              New request
            </SpaLink>
          </>
        }
      />

      <DashboardInsightStrip data={data} />

      <div className="eh-omnibox-cta" data-tour="copilot">
        <button type="button" className="eh-omnibox-cta-button" data-command-trigger>
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <circle cx="11" cy="11" r="7" />
            <path d="m20 20-3.5-3.5" />
          </svg>
          <span>Search requests, applications, and pages…</span>
          <kbd className="command-kbd" data-command-kbd>⌘K</kbd>
        </button>
        <p className="small text-muted mb-0 mt-2">
          Opens the command palette — keyword search across your portfolio, not a generative chat.
        </p>
      </div>

      {(insights.unresolvedDriftFindings > 0 || insights.staleRepositoryCount > 0) ? (
        <div className="row g-3 mb-4" data-tour="intelligence-health">
          {insights.unresolvedDriftFindings > 0 ? (
            <div className="col-md-6 col-xl-4">
              <SpaLink href="/Spa/SchemaDrift" className="text-decoration-none">
                <div className="stat-card queue-action-card stat-card-link">
                  <div className="label">Schema drift</div>
                  <div className="value text-warning">{insights.unresolvedDriftFindings}</div>
                  <div className="small text-muted">Unresolved findings → review drift</div>
                </div>
              </SpaLink>
            </div>
          ) : null}
          {insights.topDriftFindings.length > 0 ? (
            <div className="col-lg-8">
              <div className="card-panel p-3 h-100">
                <div className="d-flex justify-content-between align-items-center mb-2">
                  <h2 className="h6 mb-0">Top drift findings</h2>
                  <SpaLink href="/Spa/SchemaDrift" className="small">
                    View all
                  </SpaLink>
                </div>
                {insights.topDriftFindings.map((finding) => (
                  <div
                    key={finding.id}
                    className="d-flex justify-content-between align-items-start gap-2 py-2 border-bottom"
                  >
                    <div className="min-w-0">
                      <SpaLink href={finding.linkPath} className="small fw-semibold d-block text-truncate">
                        {finding.title}
                      </SpaLink>
                      <div className="small text-muted">
                        {finding.severity} · {finding.connectionName}
                      </div>
                    </div>
                    <SpaLink
                      href={`/Spa/CreateRequest?driftFindingId=${finding.id}`}
                      className="btn btn-sm btn-outline-primary flex-shrink-0"
                    >
                      Create request
                    </SpaLink>
                  </div>
                ))}
              </div>
            </div>
          ) : null}
          {insights.staleRepositoryCount > 0 ? (
            <div className="col-md-6 col-xl-4">
              <SpaLink href="/Spa/Repositories" className="text-decoration-none">
                <div className="stat-card queue-action-card stat-card-link">
                  <div className="label">Stale repositories</div>
                  <div className="value text-danger">{insights.staleRepositoryCount}</div>
                  <div className="small text-muted">Re-index or open system map →</div>
                </div>
              </SpaLink>
            </div>
          ) : null}
          {insights.staleRepositoryCount > 0 ? (
            <div className="col-md-6 col-xl-4">
              <SpaLink href="/Spa/SystemMap" className="text-decoration-none">
                <div className="stat-card queue-action-card stat-card-link">
                  <div className="label">Portfolio map</div>
                  <div className="value text-primary">→</div>
                  <div className="small text-muted">Explore system map</div>
                </div>
              </SpaLink>
            </div>
          ) : null}
        </div>
      ) : null}

      {(isApprover && insights.myPendingApprovals > 0) || insights.myAwaitingAnalysis > 0 ? (
        <div className="row g-3 mb-4">
          {isApprover && insights.myPendingApprovals > 0 ? (
            <div className="col-md-6 col-xl-4">
              <SpaLink href="/Spa/ApprovalQueue" className="text-decoration-none">
                <div className="stat-card queue-action-card urgent stat-card-link">
                  <div className="label">Needs your decision</div>
                  <div className="value text-danger">{insights.myPendingApprovals}</div>
                  <div className="small text-muted">Open approval queue →</div>
                </div>
              </SpaLink>
            </div>
          ) : null}
          {insights.myAwaitingAnalysis > 0 ? (
            <div className="col-md-6 col-xl-4">
              <SpaLink href="/Spa/RequestList?status=Submitted" className="text-decoration-none">
                <div className="stat-card queue-action-card stat-card-link">
                  <div className="label">Being reviewed</div>
                  <div className="value text-info">{insights.myAwaitingAnalysis}</div>
                  <div className="small text-muted">View submitted requests →</div>
                </div>
              </SpaLink>
            </div>
          ) : null}
        </div>
      ) : null}

      {showOnboardingChecklist ? (
        <div className="card-panel p-4 mb-4" data-tour="onboarding-checklist">
          <div className="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
            <div>
              <h2 className="h5 mb-1">Getting started</h2>
              <p className="text-muted mb-0">
                These setup steps help your IT team understand your systems. You can ask IT to
                complete them if you prefer.
              </p>
            </div>
            {onboardingStatus.activeSessionId ? (
              <SpaLink
                href={`/Spa/OnboardingWizard/${onboardingStatus.activeSessionId}`}
                className="btn btn-sm btn-primary"
              >
                Resume wizard
              </SpaLink>
            ) : (
              <SpaLink href="/Spa/OnboardingWizard" className="btn btn-sm btn-primary">
                Start wizard
              </SpaLink>
            )}
          </div>
          <div className="onboarding-checklist">
            <ChecklistLink done={onboardingStatus.applicationCount > 0} href="/Spa/OnboardingWizard" step="1">
              Register your first system
            </ChecklistLink>
            <ChecklistLink done={onboardingStatus.repositoryCount > 0} href="/Spa/OnboardingWizard" step="2">
              Connect source code (IT)
            </ChecklistLink>
            <ChecklistLink
              done={onboardingStatus.databaseConnectionCount > 0}
              href="/Spa/DatabaseConnections"
              step="3"
            >
              Connect a database (IT)
            </ChecklistLink>
            <ChecklistLink done={onboardingStatus.hasIndexedRepository} href="/Spa/Repositories" step="4">
              Scan code for dependencies (IT)
            </ChecklistLink>
            <ChecklistLink done={onboardingStatus.hasSystemGraph} href="/Spa/SystemMap" step="5">
              Build a system map
            </ChecklistLink>
          </div>
        </div>
      ) : null}

      {hasRequests ? (
        <div className="row g-3 mb-4" data-tour="pipeline-stats">
          <div className="col-lg-8">
            <div className="row g-3">
              <StatCard label="All requests" value={report.totalRequests} />
              <LinkedStatCard
                label="Being reviewed"
                value={report.awaitingAnalysisCount}
                href="/Spa/RequestList?status=Submitted"
                valueClass="text-info"
              />
              <LinkedStatCard
                label="Waiting for approval"
                value={report.pendingApprovalCount}
                href="/Spa/ApprovalQueue"
                valueClass="text-warning"
              />
              <StatCard
                label="Ready for IT"
                value={report.readyForDevelopmentCount}
                valueClass="text-success"
              />
            </div>
            {isApprover ? (
            <div className="row g-3 mt-0">
              <LinkedStatCard
                label="High-impact pending"
                value={report.highRiskPendingApprovalCount}
                href="/Spa/ApprovalQueue?view=highrisk"
                valueClass="text-danger"
                colClass="col-md-4"
              />
              <div className="col-md-4">
                <div className="stat-card">
                  <div className="label">Avg. decision time</div>
                  <div className="value fs-4">
                    {report.averageApprovalTimeHours?.toFixed(1) ?? '—'}
                    <span className="fs-6"> hrs</span>
                  </div>
                </div>
              </div>
              <div className="col-md-4">
                <div className="stat-card">
                  <div className="label">Last 7 days</div>
                  <div className="sparkline mt-2" role="img" aria-label="Request volume last 7 days">
                    {insights.requestsLast7Days.map((day) => {
                      const height = Math.max(8, Math.round((day.count * 100) / maxTrend));
                      return (
                        <div
                          key={day.date}
                          className={`sparkline-bar ${day.count === maxTrend ? 'active' : ''}`}
                          style={{ height: `${height}%` }}
                        />
                      );
                    })}
                  </div>
                </div>
              </div>
            </div>
            ) : null}
          </div>
          <div className="col-lg-4">
            <ActivityFeed items={insights.recentActivity} />
          </div>
        </div>
      ) : (
        <EmptyState
          title="No requests yet"
          description="Submit your first change request to see progress here."
          icon="inbox"
          action={
            <SpaLink href="/Spa/CreateRequest" className="btn btn-primary">
              Submit a request
            </SpaLink>
          }
        />
      )}
    </div>
  );
}

function ChecklistLink({
  done,
  href,
  step,
  children,
}: {
  done: boolean;
  href: string;
  step: string;
  children: ReactNode;
}) {
  return (
    <SpaLink className={`checklist-item checklist-item-link ${done ? 'done' : ''}`} href={href}>
      <span className="check">{done ? '✓' : step}</span>
      <span>{children}</span>
    </SpaLink>
  );
}

function StatCard({
  label,
  value,
  valueClass,
  colClass = 'col-sm-6 col-xl-3',
}: {
  label: string;
  value: number;
  valueClass?: string;
  colClass?: string;
}) {
  return (
    <div className={colClass}>
      <div className="stat-card">
        <div className="label">{label}</div>
        <div className={`value ${valueClass ?? ''}`.trim()}>{value}</div>
      </div>
    </div>
  );
}

function LinkedStatCard({
  label,
  value,
  href,
  valueClass,
  colClass = 'col-sm-6 col-xl-3',
}: {
  label: string;
  value: number;
  href: string;
  valueClass?: string;
  colClass?: string;
}) {
  return (
    <div className={colClass}>
      <SpaLink href={href} className="text-decoration-none">
        <div className="stat-card stat-card-link">
          <div className="label">{label}</div>
          <div className={`value ${valueClass ?? ''}`.trim()}>{value}</div>
        </div>
      </SpaLink>
    </div>
  );
}

function ActivityFeed({ items }: { items: DashboardActivityItem[] }) {
  return (
    <div className="card-panel p-3 h-100">
      <h2 className="h6 mb-3">Recent activity</h2>
      {items.length === 0 ? (
        <p className="text-muted small mb-0">No recent activity yet.</p>
      ) : (
        items.map((item) => (
          <SpaLink key={`${item.linkPath}-${item.occurredAt}`} href={item.linkPath} className="activity-feed-item">
            <span className="activity-dot" />
            <div>
              <div className="small fw-semibold">{item.title}</div>
              <div className="small text-muted">
                {item.subtitle ?? item.eventType} · {new Date(item.occurredAt).toLocaleString()}
              </div>
            </div>
          </SpaLink>
        ))
      )}
    </div>
  );
}

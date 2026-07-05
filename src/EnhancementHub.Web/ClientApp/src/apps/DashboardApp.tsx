import { FormEvent, useEffect, useMemo, useState, type ReactNode } from 'react';
import { getDashboard, searchPipeline } from '../api/spaClient';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import type { DashboardPageData, DashboardActivityItem } from '../types/spa';

interface SearchResult {
  type?: string;
  title: string;
  subtitle?: string;
  url: string;
}

export function DashboardApp() {
  const [data, setData] = useState<DashboardPageData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [searchAnswer, setSearchAnswer] = useState('');
  const [searchResults, setSearchResults] = useState<SearchResult[]>([]);
  const [searching, setSearching] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);
      try {
        const dashboard = await getDashboard();
        if (!cancelled) {
          setData(dashboard);
        }
      } catch {
        if (!cancelled) {
          setError('Failed to load dashboard.');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
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

  async function handleSearch(event: FormEvent) {
    event.preventDefault();
    const query = searchQuery.trim();
    if (!query) {
      return;
    }

    setSearching(true);
    setSearchAnswer('');
    setSearchResults([]);
    try {
      const response = await fetch(`/web-api/ux/copilot?q=${encodeURIComponent(query)}`, {
        credentials: 'include',
      });
      const payload = (await response.json()) as {
        answer?: string;
        items?: SearchResult[];
      };
      setSearchAnswer(payload.answer ?? '');
      setSearchResults(Array.isArray(payload.items) ? payload.items : []);
    } catch {
      try {
        setSearchResults(await searchPipeline(query));
      } catch {
        setSearchAnswer('Unable to run search.');
      }
    } finally {
      setSearching(false);
    }
  }

  if (loading) {
    return (
      <div aria-busy="true">
        <p className="text-muted" role="status">
          Loading dashboard…
        </p>
        <LoadingSkeleton />
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="alert alert-danger" role="alert">
        {error ?? 'Dashboard unavailable.'}
      </div>
    );
  }

  const { report, insights, onboardingStatus, isApprover, showOnboardingChecklist } = data;
  const hasRequests = report.totalRequests > 0;

  return (
    <div aria-live="polite">
      <div
        className="page-header d-flex justify-content-between align-items-center flex-wrap gap-2"
        data-tour="dashboard-header"
      >
        <div>
          <h1>Dashboard</h1>
          <p className="mb-0">Your enhancement pipeline control room</p>
        </div>
        <div className="d-flex gap-2">
          <a href="/Spa/OnboardingWizard" className="btn btn-outline-primary">
            Document an application
          </a>
          <a href="/Spa/CreateRequest" className="btn btn-primary" data-tour="new-request">
            New Request
          </a>
        </div>
      </div>

      <div className="copilot-bar" data-tour="copilot">
        <form className="d-flex gap-2 flex-wrap align-items-center" onSubmit={(e) => void handleSearch(e)}>
          <label htmlFor="copilot-input" className="visually-hidden">
            Pipeline search
          </label>
          <span className="fw-semibold text-nowrap">Pipeline search</span>
          <input
            type="search"
            id="copilot-input"
            className="form-control flex-grow-1"
            placeholder="e.g. high risk pending approval, system map…"
            autoComplete="off"
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
          />
          <button type="submit" className="btn btn-primary" disabled={searching}>
            {searching ? 'Searching…' : 'Search'}
          </button>
        </form>
        <p className="small text-muted mb-0 mt-1">
          Keyword shortcuts across requests, pages, and applications — not a generative AI chat.
        </p>
        <div id="copilot-results" className="mt-2" aria-live="polite">
          {searchAnswer ? <p className="small fw-semibold mb-2">{searchAnswer}</p> : null}
          {searchResults.map((item) => (
            <a key={`${item.url}-${item.title}`} href={item.url} className="copilot-result d-block">
              <strong>{item.title}</strong>
              {item.subtitle ? <span className="small text-muted d-block">{item.subtitle}</span> : null}
            </a>
          ))}
        </div>
      </div>

      {(isApprover && insights.myPendingApprovals > 0) || insights.myAwaitingAnalysis > 0 ? (
        <div className="row g-3 mb-4">
          {isApprover && insights.myPendingApprovals > 0 ? (
            <div className="col-md-6 col-xl-4">
              <a href="/Spa/ApprovalQueue" className="text-decoration-none">
                <div className="stat-card queue-action-card urgent stat-card-link">
                  <div className="label">Needs your approval</div>
                  <div className="value text-danger">{insights.myPendingApprovals}</div>
                  <div className="small text-muted">Open approval queue →</div>
                </div>
              </a>
            </div>
          ) : null}
          {insights.myAwaitingAnalysis > 0 ? (
            <div className="col-md-6 col-xl-4">
              <a href="/EnhancementRequests/Index?status=Submitted" className="text-decoration-none">
                <div className="stat-card queue-action-card stat-card-link">
                  <div className="label">Awaiting AI analysis</div>
                  <div className="value text-info">{insights.myAwaitingAnalysis}</div>
                  <div className="small text-muted">View submitted requests →</div>
                </div>
              </a>
            </div>
          ) : null}
        </div>
      ) : null}

      {showOnboardingChecklist ? (
        <div className="card-panel p-4 mb-4" data-tour="onboarding-checklist">
          <div className="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-3">
            <div>
              <h2 className="h5 mb-1">Getting started</h2>
              <p className="text-muted mb-0">Complete these steps to document your first application.</p>
            </div>
            {onboardingStatus.activeSessionId ? (
              <a
                href={`/Spa/OnboardingWizard/${onboardingStatus.activeSessionId}`}
                className="btn btn-sm btn-primary"
              >
                Resume wizard
              </a>
            ) : (
              <a href="/Spa/OnboardingWizard" className="btn btn-sm btn-primary">
                Start wizard
              </a>
            )}
          </div>
          <div className="onboarding-checklist">
            <ChecklistLink done={onboardingStatus.applicationCount > 0} href="/Spa/OnboardingWizard" step="1">
              Register an application
            </ChecklistLink>
            <ChecklistLink done={onboardingStatus.repositoryCount > 0} href="/Spa/OnboardingWizard" step="2">
              Connect a repository
            </ChecklistLink>
            <ChecklistLink
              done={onboardingStatus.databaseConnectionCount > 0}
              href="/DatabaseConnections/Index"
              step="3"
            >
              Connect a database
            </ChecklistLink>
            <ChecklistLink done={onboardingStatus.hasIndexedRepository} href="/Repositories/Index" step="4">
              Run code discovery
            </ChecklistLink>
            <ChecklistLink done={onboardingStatus.hasSystemGraph} href="/Spa/SystemMap" step="5">
              Build system map &amp; export docs
            </ChecklistLink>
          </div>
        </div>
      ) : null}

      {hasRequests ? (
        <div className="row g-3 mb-4" data-tour="pipeline-stats">
          <div className="col-lg-8">
            <div className="row g-3">
              <StatCard label="Total Requests" value={report.totalRequests} />
              <LinkedStatCard
                label="Awaiting Analysis"
                value={report.awaitingAnalysisCount}
                href="/EnhancementRequests/Index?status=Submitted"
                valueClass="text-info"
              />
              <LinkedStatCard
                label="Pending Approval"
                value={report.pendingApprovalCount}
                href="/Spa/ApprovalQueue"
                valueClass="text-warning"
              />
              <StatCard
                label="Ready for Dev"
                value={report.readyForDevelopmentCount}
                valueClass="text-success"
              />
            </div>
            <div className="row g-3 mt-0">
              <LinkedStatCard
                label="High-Risk Pending"
                value={report.highRiskPendingApprovalCount}
                href="/Spa/ApprovalQueue?view=highrisk"
                valueClass="text-danger"
                colClass="col-md-4"
              />
              <div className="col-md-4">
                <div className="stat-card">
                  <div className="label">Avg Approval Time</div>
                  <div className="value fs-4">
                    {report.averageApprovalTimeHours?.toFixed(1) ?? '—'}
                    <span className="fs-6"> hrs</span>
                  </div>
                </div>
              </div>
              <div className="col-md-4">
                <div className="stat-card">
                  <div className="label">7-day volume</div>
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
          </div>
          <div className="col-lg-4">
            <ActivityFeed items={insights.recentActivity} />
          </div>
        </div>
      ) : (
        <div className="card-panel empty-state">
          <div className="empty-state-icon">☰</div>
          <h2 className="h5">No requests yet</h2>
          <p className="mb-3">Submit your first enhancement request to populate the dashboard.</p>
          <a href="/Spa/CreateRequest" className="btn btn-primary">
            Create request
          </a>
        </div>
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
    <a className={`checklist-item checklist-item-link ${done ? 'done' : ''}`} href={href}>
      <span className="check">{done ? '✓' : step}</span>
      <span>{children}</span>
    </a>
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
      <a href={href} className="text-decoration-none">
        <div className="stat-card stat-card-link">
          <div className="label">{label}</div>
          <div className={`value ${valueClass ?? ''}`.trim()}>{value}</div>
        </div>
      </a>
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
          <a key={`${item.linkPath}-${item.occurredAt}`} href={item.linkPath} className="activity-feed-item">
            <span className="activity-dot" />
            <div>
              <div className="small fw-semibold">{item.title}</div>
              <div className="small text-muted">
                {item.subtitle ?? item.eventType} · {new Date(item.occurredAt).toLocaleString()}
              </div>
            </div>
          </a>
        ))
      )}
    </div>
  );
}

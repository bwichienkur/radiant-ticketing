import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import { listEnhancementRequests } from '../api/spaClient';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { riskBadgeClass } from '../components/MissionControl';
import type { EnhancementRequestListItem } from '../types/spa';

const PRIORITIES = ['Critical', 'High', 'Medium', 'Low'];
const STATUSES = [
  'Submitted',
  'AiAnalyzing',
  'NeedsClarification',
  'PendingApproval',
  'Approved',
  'Rejected',
  'ReadyForDevelopment',
  'InProgress',
  'Completed',
  'Cancelled',
];
const SORT_OPTIONS = [
  { value: 'Newest', label: 'Newest first' },
  { value: 'Oldest', label: 'Oldest first' },
  { value: 'HighestRisk', label: 'Highest risk' },
  { value: 'Priority', label: 'Priority' },
];

interface ListFilters {
  q: string;
  status: string;
  priority: string;
  view: string;
  sort: string;
}

function readFiltersFromUrl(): ListFilters {
  const params = new URLSearchParams(window.location.search);
  return {
    q: params.get('q') ?? '',
    status: params.get('status') ?? '',
    priority: params.get('priority') ?? '',
    view: params.get('view') ?? '',
    sort: params.get('sort') ?? 'Newest',
  };
}

function filtersToSearchParams(filters: ListFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.q.trim()) {
    params.set('q', filters.q.trim());
  }
  if (filters.status) {
    params.set('status', filters.status);
  }
  if (filters.priority) {
    params.set('priority', filters.priority);
  }
  if (filters.view) {
    params.set('view', filters.view);
  }
  if (filters.sort && filters.sort !== 'Newest') {
    params.set('sort', filters.sort);
  }
  return params;
}

const STATUS_BY_VALUE: Record<number, string> = {
  0: 'Submitted',
  1: 'AiAnalyzing',
  2: 'NeedsClarification',
  3: 'PendingApproval',
  4: 'Approved',
  5: 'Rejected',
  6: 'ReadyForDevelopment',
  7: 'InProgress',
  8: 'Completed',
  9: 'Cancelled',
};

const RISK_BY_VALUE: Record<number, string> = {
  0: 'Low',
  1: 'Medium',
  2: 'High',
  3: 'Critical',
};

function normalizeStatus(status: string | number): string {
  if (typeof status === 'number') {
    return STATUS_BY_VALUE[status] ?? String(status);
  }

  if (/^\d+$/.test(status)) {
    return STATUS_BY_VALUE[Number(status)] ?? status;
  }

  return status;
}

function normalizeRisk(risk: string | number | undefined): string | undefined {
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

function formatStatus(status: string | number): string {
  return normalizeStatus(status).replace(/([a-z])([A-Z])/g, '$1 $2');
}

export function RequestListApp() {
  const [filters, setFilters] = useState<ListFilters>(readFiltersFromUrl);
  const [draftFilters, setDraftFilters] = useState<ListFilters>(readFiltersFromUrl);
  const [requests, setRequests] = useState<EnhancementRequestListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadRequests = useCallback(async (activeFilters: ListFilters) => {
    setLoading(true);
    setError(null);
    try {
      const items = await listEnhancementRequests({
        q: activeFilters.q || undefined,
        status: activeFilters.status || undefined,
        priority: activeFilters.priority || undefined,
        view: activeFilters.view || undefined,
        sort: activeFilters.sort || 'Newest',
      });
      setRequests(items);
    } catch {
      setError('Failed to load enhancement requests.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadRequests(filters);
  }, [filters, loadRequests]);

  const chipHref = useMemo(() => {
    return (chip: Partial<ListFilters>) => {
      const next = { q: '', status: '', priority: '', view: '', sort: filters.sort, ...chip };
      const params = filtersToSearchParams(next);
      const query = params.toString();
      return query ? `/Spa/RequestList?${query}` : '/Spa/RequestList';
    };
  }, [filters.sort]);

  function applyFilters(event: FormEvent) {
    event.preventDefault();
    const next = { ...draftFilters };
    setFilters(next);
    const params = filtersToSearchParams(next);
    const query = params.toString();
    const url = query ? `/Spa/RequestList?${query}` : '/Spa/RequestList';
    window.history.replaceState(null, '', url);
  }

  function isChipActive(chip: Partial<ListFilters>): boolean {
    if (chip.view === 'highrisk') {
      return filters.view === 'highrisk';
    }
    if (chip.status === 'PendingApproval') {
      return filters.status === 'PendingApproval' && !filters.view;
    }
    if (chip.status === 'Submitted') {
      return filters.status === 'Submitted' && !filters.view;
    }
    return !filters.status && !filters.view && !filters.q && !filters.priority;
  }

  return (
    <>
      <div className="page-header d-flex justify-content-between align-items-center flex-wrap gap-2">
        <div>
          <h1>Enhancement Requests</h1>
          <p>Triage, search, and open request details</p>
        </div>
        <a href="/Spa/CreateRequest" className="btn btn-primary">
          New Request
        </a>
      </div>

      <form className="card-panel p-3 mb-3" onSubmit={applyFilters}>
        <div className="row g-2 align-items-end">
          <div className="col-md-4">
            <label className="form-label small" htmlFor="search-q">
              Search
            </label>
            <input
              type="search"
              className="form-control"
              id="search-q"
              value={draftFilters.q}
              onChange={(event) => setDraftFilters((prev) => ({ ...prev, q: event.target.value }))}
              placeholder="Title, submitter, application…"
            />
          </div>
          <div className="col-md-2">
            <label className="form-label small" htmlFor="filter-status">
              Status
            </label>
            <select
              className="form-select"
              id="filter-status"
              value={draftFilters.status}
              onChange={(event) => setDraftFilters((prev) => ({ ...prev, status: event.target.value }))}
            >
              <option value="">All</option>
              {STATUSES.map((status) => (
                <option key={status} value={status}>
                  {formatStatus(status)}
                </option>
              ))}
            </select>
          </div>
          <div className="col-md-2">
            <label className="form-label small" htmlFor="filter-priority">
              Priority
            </label>
            <select
              className="form-select"
              id="filter-priority"
              value={draftFilters.priority}
              onChange={(event) => setDraftFilters((prev) => ({ ...prev, priority: event.target.value }))}
            >
              <option value="">All</option>
              {PRIORITIES.map((priority) => (
                <option key={priority} value={priority}>
                  {priority}
                </option>
              ))}
            </select>
          </div>
          <div className="col-md-2">
            <label className="form-label small" htmlFor="filter-sort">
              Sort
            </label>
            <select
              className="form-select"
              id="filter-sort"
              value={draftFilters.sort}
              onChange={(event) => setDraftFilters((prev) => ({ ...prev, sort: event.target.value }))}
            >
              {SORT_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
          <div className="col-md-2">
            <button type="submit" className="btn btn-primary w-100">
              Apply
            </button>
          </div>
        </div>
        <div className="filter-chips mt-3">
          <a
            className={`filter-chip ${isChipActive({}) ? 'active' : ''}`}
            href={chipHref({ q: '', status: '', priority: '', view: '' })}
          >
            All
          </a>
          <a
            className={`filter-chip ${isChipActive({ status: 'PendingApproval' }) ? 'active' : ''}`}
            href={chipHref({ q: '', status: 'PendingApproval', priority: '', view: '' })}
          >
            Pending approval
          </a>
          <a
            className={`filter-chip ${isChipActive({ status: 'Submitted' }) ? 'active' : ''}`}
            href={chipHref({ q: '', status: 'Submitted', priority: '', view: '' })}
          >
            Awaiting analysis
          </a>
          <a
            className={`filter-chip ${isChipActive({ view: 'highrisk' }) ? 'active' : ''}`}
            href={chipHref({ q: '', status: '', priority: '', view: 'highrisk' })}
          >
            High risk
          </a>
        </div>
      </form>

      {loading ? (
        <LoadingSkeleton />
      ) : error ? (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      ) : requests.length === 0 ? (
        <div className="card-panel empty-state">
          <div className="empty-state-icon">☰</div>
          <h2 className="h5">No requests match your filters</h2>
          <p className="mb-3">Try clearing filters or create a new enhancement request.</p>
          <a href="/Spa/CreateRequest" className="btn btn-primary">
            New Request
          </a>
        </div>
      ) : (
        <>
          <div className="card-panel table-desktop-only">
            <div className="table-responsive">
              <table className="table table-hover table-enterprise mb-0">
                <thead>
                  <tr>
                    <th scope="col">Title</th>
                    <th scope="col">Application</th>
                    <th scope="col">Risk</th>
                    <th scope="col">Priority</th>
                    <th scope="col">Status</th>
                    <th scope="col">Submitted By</th>
                    <th scope="col">Age</th>
                    <th scope="col"></th>
                  </tr>
                </thead>
                <tbody>
                  {requests.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <strong>{item.title}</strong>
                      </td>
                      <td>{item.targetApplicationName ?? '—'}</td>
                      <td>
                        {item.latestRiskLevel != null ? (
                          <span
                            className={`badge ${riskBadgeClass(normalizeRisk(item.latestRiskLevel)!)} badge-status`}
                          >
                            {normalizeRisk(item.latestRiskLevel)}
                          </span>
                        ) : (
                          <span className="text-muted">—</span>
                        )}
                      </td>
                      <td>{item.priority}</td>
                      <td>
                        <span className="badge text-bg-secondary badge-status">{formatStatus(item.status)}</span>
                      </td>
                      <td>{item.submittedByUserName ?? '—'}</td>
                      <td>{item.daysInStatus ?? 0}d</td>
                      <td>
                        <a href={`/Spa/RequestDetail/${item.id}`} className="btn btn-sm btn-outline-primary">
                          View
                        </a>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="cards-mobile-only">
            {requests.map((item) => (
              <a
                key={item.id}
                href={`/Spa/RequestDetail/${item.id}`}
                className={`request-card-mobile ${
                  normalizeStatus(item.status) === 'PendingApproval' ? 'status-pending' : ''
                } d-block text-decoration-none text-reset`}
              >
                <div className="d-flex justify-content-between align-items-start gap-2 mb-1">
                  <strong>{item.title}</strong>
                  {item.latestRiskLevel != null ? (
                    <span
                      className={`badge ${riskBadgeClass(normalizeRisk(item.latestRiskLevel)!)} badge-status`}
                    >
                      {normalizeRisk(item.latestRiskLevel)}
                    </span>
                  ) : null}
                </div>
                <div className="small text-muted">
                  {formatStatus(item.status)} · {item.priority} · {item.daysInStatus ?? 0}d
                </div>
              </a>
            ))}
          </div>
        </>
      )}
    </>
  );
}

import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import { exportEnhancementRequests, listEnhancementRequests } from '../api/spaClient';
import {
  EmptyState,
  ErrorState,
  ListToolbar,
  LoadingState,
  PageHeader,
  Pagination,
  StatusBadge,
  useToast,
} from '../components/ui';
import type { EnhancementRequestListItem } from '../types/spa';
import { formatRequestStatus, normalizeRequestStatus } from '../utils/requestLabels';

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


function buildFilterSummary(filters: ListFilters): string | undefined {
  const parts: string[] = [];
  if (filters.q.trim()) {
    parts.push(`search “${filters.q.trim()}”`);
  }
  if (filters.status) {
    parts.push(formatRequestStatus(filters.status));
  }
  if (filters.priority) {
    parts.push(filters.priority);
  }
  if (filters.view === 'highrisk') {
    parts.push('high risk');
  }
  return parts.length > 0 ? parts.join(', ') : undefined;
}

const DEFAULT_PAGE_SIZE = 25;

export function RequestListApp() {
  const toast = useToast();
  const [filters, setFilters] = useState<ListFilters>(readFiltersFromUrl);
  const [draftFilters, setDraftFilters] = useState<ListFilters>(readFiltersFromUrl);
  const [requests, setRequests] = useState<EnhancementRequestListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(() => new Set());

  const loadRequests = useCallback(
    async (activeFilters: ListFilters, activePage: number, activePageSize: number) => {
      setLoading(true);
      setError(null);
      try {
        const result = await listEnhancementRequests({
          q: activeFilters.q || undefined,
          status: activeFilters.status || undefined,
          priority: activeFilters.priority || undefined,
          view: activeFilters.view || undefined,
          sort: activeFilters.sort || 'Newest',
          page: activePage,
          pageSize: activePageSize,
        });
        setRequests(result.items);
        setTotalCount(result.totalCount);
      } catch {
        setError('Failed to load enhancement requests.');
      } finally {
        setLoading(false);
      }
    },
    [],
  );

  useEffect(() => {
    void loadRequests(filters, page, pageSize);
  }, [filters, page, pageSize, loadRequests]);

  useEffect(() => {
    setPage(1);
    setSelectedIds(new Set());
  }, [filters]);

  const allPageSelected =
    requests.length > 0 && requests.every((item) => selectedIds.has(item.id));

  function toggleSelectAllOnPage() {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (allPageSelected) {
        requests.forEach((item) => next.delete(item.id));
      } else {
        requests.forEach((item) => next.add(item.id));
      }
      return next;
    });
  }

  function toggleSelect(id: string) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  const selectedPendingCount = useMemo(
    () =>
      requests.filter(
        (item) =>
          selectedIds.has(item.id) && normalizeRequestStatus(item.status) === 'PendingApproval',
      ).length,
    [requests, selectedIds],
  );

  async function handleExportSelected() {
    if (selectedIds.size === 0) {
      return;
    }

    setExporting(true);
    try {
      await exportEnhancementRequests([...selectedIds]);
      toast.success('Export ready', 'Your CSV download should begin shortly.');
    } catch {
      toast.danger('Export failed', 'Could not export the selected requests.');
    } finally {
      setExporting(false);
    }
  }

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
      <PageHeader
        title="Enhancement Requests"
        description="Triage, search, and open request details"
        actions={
          <a href="/Spa/CreateRequest" className="btn btn-primary">
            New request
          </a>
        }
      />

      <form className="card-panel p-3 mb-3 eh-filter-panel" onSubmit={applyFilters} aria-label="Filter requests">
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
                  {formatRequestStatus(status)}
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
        <LoadingState label="Loading requests…" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void loadRequests(filters, page, pageSize)} />
      ) : totalCount === 0 ? (
        <EmptyState
          title="No requests match your filters"
          description="Try clearing filters or create a new enhancement request."
          icon="search"
          action={
            <>
              <a href="/Spa/CreateRequest" className="btn btn-primary me-2">
                New request
              </a>
              <a href="/Spa/RequestList" className="btn btn-outline-secondary">
                Clear filters
              </a>
            </>
          }
        />
      ) : (
        <>
          <ListToolbar
            count={totalCount}
            noun="request"
            filterSummary={buildFilterSummary(filters)}
          />
          <div className="card-panel table-desktop-only">
            {selectedIds.size > 0 ? (
              <div className="eh-bulk-toolbar" role="toolbar" aria-label="Bulk actions">
                <span className="small fw-semibold">{selectedIds.size} selected</span>
                {selectedPendingCount > 0 ? (
                  <a href="/Spa/ApprovalQueue" className="btn btn-sm btn-primary">
                    Review in approval queue
                  </a>
                ) : null}
                <button
                  type="button"
                  className="btn btn-sm btn-outline-primary"
                  disabled={exporting}
                  onClick={() => void handleExportSelected()}
                >
                  {exporting ? 'Exporting…' : 'Export CSV'}
                </button>
                <button
                  type="button"
                  className="btn btn-sm btn-outline-secondary"
                  onClick={() => setSelectedIds(new Set())}
                >
                  Clear selection
                </button>
              </div>
            ) : null}
            <div className="table-responsive">
              <table className="table table-hover table-enterprise mb-0">
                <thead>
                  <tr>
                    <th scope="col" className="eh-table-checkbox-col">
                      <input
                        type="checkbox"
                        className="form-check-input"
                        checked={allPageSelected}
                        onChange={toggleSelectAllOnPage}
                        aria-label="Select all on this page"
                      />
                    </th>
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
                        <input
                          type="checkbox"
                          className="form-check-input"
                          checked={selectedIds.has(item.id)}
                          onChange={() => toggleSelect(item.id)}
                          aria-label={`Select ${item.title}`}
                        />
                      </td>
                      <td>
                        <strong>{item.title}</strong>
                      </td>
                      <td>{item.targetApplicationName ?? '—'}</td>
                      <td>
                        <StatusBadge risk={item.latestRiskLevel} />
                      </td>
                      <td>{item.priority}</td>
                      <td>
                        <StatusBadge status={item.status} />
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
            <Pagination
              page={page}
              pageSize={pageSize}
              totalCount={totalCount}
              onPageChange={setPage}
              onPageSizeChange={(size) => {
                setPageSize(size);
                setPage(1);
              }}
            />
          </div>

          <div className="cards-mobile-only">
            {requests.map((item) => (
              <a
                key={item.id}
                href={`/Spa/RequestDetail/${item.id}`}
                className={`request-card-mobile ${
                  normalizeRequestStatus(item.status) === 'PendingApproval' ? 'status-pending' : ''
                } d-block text-decoration-none text-reset`}
              >
                <div className="d-flex justify-content-between align-items-start gap-2 mb-1">
                  <strong>{item.title}</strong>
                  <StatusBadge risk={item.latestRiskLevel} />
                </div>
                <div className="small text-muted">
                  {formatRequestStatus(item.status)} · {item.priority} · {item.daysInStatus ?? 0}d
                </div>
              </a>
            ))}
            <Pagination
              page={page}
              pageSize={pageSize}
              totalCount={totalCount}
              onPageChange={setPage}
              onPageSizeChange={(size) => {
                setPageSize(size);
                setPage(1);
              }}
            />
          </div>
        </>
      )}
    </>
  );
}

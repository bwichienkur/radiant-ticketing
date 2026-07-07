import { FormEvent, useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { exportAuditLogs, listAuditLogs } from '../api/spaClient';
import { EmptyState, ErrorState, LoadingState, PageHeader, ResponsiveDataList } from '../components/ui';
import { readSpaContext } from '../spaRoutes';
import type { AuditLogEntry, AuditLogFilters } from '../types/spa';

function readFiltersFromUrl(params: URLSearchParams): AuditLogFilters {
  return {
    entityType: params.get('entityType') ?? '',
    action: params.get('action') ?? '',
    from: params.get('from') ?? '',
    to: params.get('to') ?? '',
  };
}

function filtersToSearchParams(filters: AuditLogFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.entityType?.trim()) {
    params.set('entityType', filters.entityType.trim());
  }
  if (filters.action?.trim()) {
    params.set('action', filters.action.trim());
  }
  if (filters.from) {
    params.set('from', filters.from);
  }
  if (filters.to) {
    params.set('to', filters.to);
  }
  return params;
}

export function AuditApp() {
  const { isAdmin } = readSpaContext();
  const [searchParams, setSearchParams] = useSearchParams();
  const [filters, setFilters] = useState<AuditLogFilters>(() => readFiltersFromUrl(searchParams));
  const [draftFilters, setDraftFilters] = useState<AuditLogFilters>(() => readFiltersFromUrl(searchParams));
  const [logs, setLogs] = useState<AuditLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadLogs = useCallback(async (activeFilters: AuditLogFilters) => {
    setLoading(true);
    setError(null);
    try {
      setLogs(await listAuditLogs(activeFilters));
    } catch {
      setError('Failed to load audit log.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const active = readFiltersFromUrl(searchParams);
    setFilters(active);
    setDraftFilters(active);
    void loadLogs(active);
  }, [loadLogs, searchParams]);

  function applyFilters(event: FormEvent) {
    event.preventDefault();
    const params = filtersToSearchParams(draftFilters);
    setSearchParams(params);
  }

  return (
    <div aria-live="polite">
      <PageHeader
        title="Audit log"
        description="System activity and change history"
        actions={
          isAdmin ? (
            <>
              <button
                type="button"
                className="btn btn-outline-primary btn-sm"
                onClick={() => exportAuditLogs('csv', filters)}
              >
                Export CSV
              </button>
              <button
                type="button"
                className="btn btn-outline-secondary btn-sm"
                onClick={() => exportAuditLogs('json', filters)}
              >
                Export JSON
              </button>
            </>
          ) : undefined
        }
      />

      <form
        className="card-panel p-3 mb-3 eh-filter-panel"
        onSubmit={applyFilters}
        aria-label="Filter audit log"
      >
        <div className="row g-2 align-items-end">
          <div className="col-md-3">
            <label className="form-label small" htmlFor="audit-entity-type">
              Entity type
            </label>
            <input
              className="form-control"
              id="audit-entity-type"
              value={draftFilters.entityType ?? ''}
              onChange={(event) =>
                setDraftFilters((current) => ({ ...current, entityType: event.target.value }))
              }
              placeholder="EnhancementRequest"
            />
          </div>
          <div className="col-md-3">
            <label className="form-label small" htmlFor="audit-action">
              Action
            </label>
            <input
              className="form-control"
              id="audit-action"
              value={draftFilters.action ?? ''}
              onChange={(event) =>
                setDraftFilters((current) => ({ ...current, action: event.target.value }))
              }
              placeholder="Approved"
            />
          </div>
          <div className="col-md-2">
            <label className="form-label small" htmlFor="audit-from">
              From
            </label>
            <input
              className="form-control"
              id="audit-from"
              type="date"
              value={draftFilters.from ?? ''}
              onChange={(event) =>
                setDraftFilters((current) => ({ ...current, from: event.target.value }))
              }
            />
          </div>
          <div className="col-md-2">
            <label className="form-label small" htmlFor="audit-to">
              To
            </label>
            <input
              className="form-control"
              id="audit-to"
              type="date"
              value={draftFilters.to ?? ''}
              onChange={(event) =>
                setDraftFilters((current) => ({ ...current, to: event.target.value }))
              }
            />
          </div>
          <div className="col-md-2">
            <button type="submit" className="btn btn-primary w-100">
              Apply filters
            </button>
          </div>
        </div>
      </form>

      {loading ? (
        <LoadingState label="Loading audit log…" />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void loadLogs(filters)} />
      ) : logs.length === 0 ? (
        <EmptyState
          title="No audit entries"
          description="No audit entries match the current filters."
          icon="search"
        />
      ) : (
        <ResponsiveDataList
          items={logs}
          getRowKey={(log) => log.id}
          columns={[
            {
              id: 'time',
              header: 'Time (UTC)',
              cell: (log) => <span className="small">{new Date(log.createdAt).toLocaleString()}</span>,
            },
            { id: 'action', header: 'Action', cell: (log) => log.action },
            { id: 'entity', header: 'Entity', cell: (log) => log.entityType },
            { id: 'user', header: 'User', cell: (log) => log.userName ?? '—' },
            {
              id: 'details',
              header: 'Details',
              cell: (log) => <span className="small">{log.comments ?? '—'}</span>,
            },
          ]}
          renderMobileCard={(log) => (
            <>
              <div className="mobile-data-card-title">{log.action}</div>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">Time</span>
                <span className="small">{new Date(log.createdAt).toLocaleString()}</span>
              </div>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">Entity</span>
                <span>{log.entityType}</span>
              </div>
              <div className="mobile-data-card-row">
                <span className="mobile-data-card-label">User</span>
                <span>{log.userName ?? '—'}</span>
              </div>
              {log.comments ? (
                <div className="small text-muted mt-2">{log.comments}</div>
              ) : null}
            </>
          )}
        />
      )}
    </div>
  );
}

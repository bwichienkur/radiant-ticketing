import type { ReactNode } from 'react';
import { useCallback, useEffect, useState } from 'react';
import { getAdminJobs, retryAdminJob } from '../../api/spaClient';
import {
  AlertBanner,
  ErrorState,
  LoadingState,
  SectionCard,
  useToast,
} from '../../components/ui';
import type { AdminJobsResponse } from '../../types/spa';

export function AdminJobsSection() {
  const toast = useToast();
  const [data, setData] = useState<AdminJobsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [retryingId, setRetryingId] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setData(await getAdminJobs());
    } catch {
      setError('Failed to load background jobs.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function handleRetry(jobId: string) {
    setRetryingId(jobId);
    try {
      const result = await retryAdminJob(jobId);
      toast.success('Job requeued', result.message);
      await reload();
    } catch {
      toast.danger('Retry failed', 'Could not requeue the failed job.');
    } finally {
      setRetryingId(null);
    }
  }

  if (loading) {
    return <LoadingState label="Loading background jobs…" />;
  }

  if (error || !data) {
    return <ErrorState message={error ?? 'Unable to load jobs.'} onRetry={() => void reload()} />;
  }

  const { status, freshness } = data;

  return (
    <div aria-live="polite">
      <h2 className="eh-section-title mb-1">Background jobs</h2>
      <p className="text-muted small mb-3">
        Queue depth, schedules, and failed job recovery · Provider: {status.provider}
      </p>

      <div className="row g-3 mb-4">
        <StatCard label="Pending indexing" value={status.queueCounts.pendingRepositoryIndexing} />
        <StatCard label="Awaiting AI analysis" value={status.queueCounts.awaitingAiAnalysis} />
        <StatCard label="Discovery queued" value={status.queueCounts.queuedApplicationDiscovery} />
        <StatCard label="Schema scans pending" value={status.queueCounts.pendingDatabaseSchemaScans} />
      </div>

      <SectionCard title={`Index freshness (${freshness.slaHours}h SLA)`} className="mb-4">
        <div className="row g-3">
          <StatCard label="Freshness" value={`${freshness.freshnessPercent}%`} />
          <StatCard label="Fresh repos" value={`${freshness.freshCount} / ${freshness.totalRepositories}`} />
          <StatCard label="Stale repos" value={freshness.staleCount} />
          <StatCard
            label="SLA status"
            value={freshness.slaMet ? 'Met' : 'Missed'}
            valueClassName={freshness.slaMet ? 'text-success' : 'text-danger'}
          />
        </div>
      </SectionCard>

      {status.hangfire ? (
        <SectionCard title="Hangfire statistics" className="mb-4">
          <div className="row g-3">
            <StatCard label="Enqueued" value={status.hangfire.enqueued} />
            <StatCard label="Processing" value={status.hangfire.processing} />
            <StatCard label="Failed" value={status.hangfire.failed} valueClassName="text-danger" />
            <StatCard label="Succeeded" value={status.hangfire.succeeded} valueClassName="text-success" />
          </div>
        </SectionCard>
      ) : status.provider.toLowerCase() === 'hangfire' ? (
        <AlertBanner variant="warning" className="mb-4">
          Hangfire statistics are unavailable from this process. View the Worker dashboard at /hangfire in
          Development.
        </AlertBanner>
      ) : null}

      <SectionCard title="Scheduled jobs" className="mb-4">
        <JobTable
          headers={['Job', 'Description', 'Schedule', 'Last run', 'Next run']}
          rows={status.jobs.map((job) => [
            <code key="id">{job.jobId}</code>,
            job.description,
            job.schedule,
            job.lastExecution ?? '—',
            job.nextExecution ?? '—',
          ])}
        />
      </SectionCard>

      <SectionCard title="Failed jobs">
        {status.failedJobs.length === 0 ? (
          <p className="text-muted mb-0">No failed jobs recorded.</p>
        ) : (
          <div className="table-responsive">
            <table className="table table-enterprise mb-0">
              <thead>
                <tr>
                  <th scope="col">Job ID</th>
                  <th scope="col">Name</th>
                  <th scope="col">Failed at</th>
                  <th scope="col">Error</th>
                  <th scope="col" />
                </tr>
              </thead>
              <tbody>
                {status.failedJobs.map((failed) => (
                  <tr key={failed.jobId}>
                    <td>
                      <code className="small">{failed.jobId}</code>
                    </td>
                    <td className="small">{failed.jobName ?? '—'}</td>
                    <td className="small">{failed.failedAt ?? '—'}</td>
                    <td className="small text-muted">{failed.exceptionMessage ?? '—'}</td>
                    <td>
                      {status.provider.toLowerCase() === 'hangfire' ? (
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-primary"
                          disabled={retryingId === failed.jobId}
                          onClick={() => void handleRetry(failed.jobId)}
                        >
                          Retry
                        </button>
                      ) : null}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </SectionCard>

      <p className="text-muted small mt-3 mb-0">
        Last updated {new Date(status.generatedAtUtc).toLocaleString()} UTC
      </p>
    </div>
  );
}

function StatCard({
  label,
  value,
  valueClassName = '',
}: {
  label: string;
  value: string | number;
  valueClassName?: string;
}) {
  return (
    <div className="col-md-3 col-6">
      <div className="stat-card">
        <div className="label">{label}</div>
        <div className={`value fs-5 ${valueClassName}`.trim()}>{value}</div>
      </div>
    </div>
  );
}

function JobTable({ headers, rows }: { headers: string[]; rows: ReactNode[][] }) {
  if (rows.length === 0) {
    return <p className="text-muted mb-0">No scheduled jobs.</p>;
  }

  return (
    <div className="table-responsive">
      <table className="table table-enterprise mb-0">
        <thead>
          <tr>
            {headers.map((header) => (
              <th key={header} scope="col">
                {header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={index}>
              {row.map((cell, cellIndex) => (
                <td key={cellIndex}>{cell}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

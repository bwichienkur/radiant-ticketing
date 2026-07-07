import { useCallback, useEffect, useState } from 'react';
import { applyAdminRetention, getAdminRetention } from '../../api/spaClient';
import {
  AlertBanner,
  ErrorState,
  LoadingState,
  SectionCard,
  useToast,
} from '../../components/ui';
import type { DataRetentionResult, DataRetentionStatus } from '../../types/spa';

export function AdminRetentionSection() {
  const toast = useToast();
  const [status, setStatus] = useState<DataRetentionStatus | null>(null);
  const [lastResult, setLastResult] = useState<DataRetentionResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setStatus(await getAdminRetention());
    } catch {
      setError('Failed to load retention status.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function runRetention(dryRun: boolean) {
    if (!dryRun && !window.confirm('Apply retention purge? This cannot be undone.')) {
      return;
    }

    setBusy(true);
    try {
      const result = await applyAdminRetention(dryRun);
      setLastResult(result);
      await reload();
      toast.success(
        dryRun ? 'Preview complete' : 'Retention applied',
        dryRun
          ? `Would delete ${result.aiPromptRunsDeleted} AI runs and ${result.attachmentsDeleted} attachments.`
          : `Deleted ${result.aiPromptRunsDeleted} AI runs and ${result.attachmentsDeleted} attachments.`,
      );
    } catch {
      toast.danger('Retention failed', 'Could not run retention job.');
    } finally {
      setBusy(false);
    }
  }

  if (loading) {
    return <LoadingState label="Loading retention policies…" />;
  }

  if (error || !status) {
    return <ErrorState message={error ?? 'Unable to load retention.'} onRetry={() => void reload()} />;
  }

  return (
    <div aria-live="polite">
      <h2 className="eh-section-title mb-1">Data retention</h2>
      <p className="text-muted small mb-3">Automated purge of AI prompt runs and attachments past policy windows</p>

      {!status.enabled ? (
        <AlertBanner variant="warning" className="mb-3">
          Retention policies are disabled in this environment.
        </AlertBanner>
      ) : null}

      <SectionCard title="Policy" className="mb-4">
        <dl className="row mb-0">
          <dt className="col-sm-5">AI prompt runs retention</dt>
          <dd className="col-sm-7">{status.aiPromptRunsRetentionDays} days</dd>
          <dt className="col-sm-5">Attachments retention</dt>
          <dd className="col-sm-7">{status.attachmentsRetentionDays} days</dd>
          <dt className="col-sm-5">Eligible AI runs</dt>
          <dd className="col-sm-7">{status.eligibleAiPromptRunCount}</dd>
          <dt className="col-sm-5">Eligible attachments</dt>
          <dd className="col-sm-7">{status.eligibleAttachmentCount}</dd>
        </dl>
      </SectionCard>

      <div className="d-flex flex-wrap gap-2 mb-3">
        <button type="button" className="btn btn-outline-primary" disabled={busy} onClick={() => void runRetention(true)}>
          Preview purge
        </button>
        <button type="button" className="btn btn-danger" disabled={busy || !status.enabled} onClick={() => void runRetention(false)}>
          Apply purge
        </button>
      </div>

      {lastResult ? (
        <AlertBanner variant="neutral">
          {lastResult.dryRun ? 'Preview' : 'Applied'} at {new Date(lastResult.appliedAtUtc).toLocaleString()} —{' '}
          {lastResult.aiPromptRunsDeleted} AI runs, {lastResult.attachmentsDeleted} attachments.
        </AlertBanner>
      ) : null}
    </div>
  );
}

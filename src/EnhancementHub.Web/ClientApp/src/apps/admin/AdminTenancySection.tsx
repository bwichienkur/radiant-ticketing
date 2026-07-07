import { useCallback, useEffect, useState } from 'react';
import {
  createBillingCheckout,
  createBillingPortal,
  getAdminTenancy,
  provisionTenantSchema,
} from '../../api/spaClient';
import {
  AlertBanner,
  ErrorState,
  LoadingState,
  SectionCard,
  useToast,
} from '../../components/ui';
import type { AdminTenancyResponse } from '../../types/spa';

export function AdminTenancySection() {
  const toast = useToast();
  const [data, setData] = useState<AdminTenancyResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setData(await getAdminTenancy());
    } catch {
      setError('Failed to load tenancy information.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function openCheckout(plan: number) {
    setBusy('checkout');
    try {
      const { url } = await createBillingCheckout(plan);
      window.location.href = url;
    } catch (err) {
      toast.danger('Checkout failed', err instanceof Error ? err.message : 'Could not start checkout.');
      setBusy(null);
    }
  }

  async function openPortal() {
    setBusy('portal');
    try {
      const { url } = await createBillingPortal();
      window.location.href = url;
    } catch (err) {
      toast.danger('Portal failed', err instanceof Error ? err.message : 'Could not open billing portal.');
      setBusy(null);
    }
  }

  async function handleProvision() {
    setBusy('provision');
    try {
      const result = await provisionTenantSchema();
      toast.success('Schema provisioned', result.message);
      await reload();
    } catch (err) {
      toast.danger('Provision failed', err instanceof Error ? err.message : 'Could not provision schema.');
    } finally {
      setBusy(null);
    }
  }

  if (loading) {
    return <LoadingState label="Loading tenancy…" />;
  }

  if (error || !data) {
    return <ErrorState message={error ?? 'Unable to load tenancy.'} onRetry={() => void reload()} />;
  }

  if (data.allTenants.length > 0 && !data.billing) {
    return (
      <div>
        <h2 className="eh-section-title mb-3">Platform tenants</h2>
        <SectionCard title="All tenants">
          <div className="table-responsive">
            <table className="table table-enterprise mb-0">
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col">Slug</th>
                  <th scope="col">Plan</th>
                  <th scope="col">Region</th>
                  <th scope="col">Active</th>
                </tr>
              </thead>
              <tbody>
                {data.allTenants.map((tenant) => (
                  <tr key={tenant.id}>
                    <td>{tenant.name}</td>
                    <td>
                      <code>{tenant.slug}</code>
                    </td>
                    <td>{tenant.plan}</td>
                    <td>{tenant.region}</td>
                    <td>{tenant.isActive ? 'Yes' : 'No'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </SectionCard>
      </div>
    );
  }

  const billing = data.billing;
  if (!billing) {
    return <ErrorState message="No billing data available." onRetry={() => void reload()} />;
  }

  return (
    <div aria-live="polite">
      <h2 className="eh-section-title mb-1">Tenancy & billing</h2>
      <p className="text-muted small mb-3">Plan limits, Stripe subscription, and tenant isolation</p>

      {billing.isTrialActive && billing.trialEndsAt ? (
        <AlertBanner variant="info" className="mb-3">
          Trial active until {new Date(billing.trialEndsAt).toLocaleDateString()}.
        </AlertBanner>
      ) : null}
      {billing.isTrialExpired ? (
        <AlertBanner variant="warning" className="mb-3">
          Trial expired — upgrade to continue without limits.
        </AlertBanner>
      ) : null}
      {billing.isOverLimit ? (
        <AlertBanner variant="danger" className="mb-3">
          Usage exceeds plan limits. Upgrade or reduce consumption.
        </AlertBanner>
      ) : null}

      <div className="row g-3 mb-4">
        <div className="col-md-4">
          <div className="stat-card">
            <div className="label">Plan</div>
            <div className="value fs-5">{billing.plan}</div>
            <div className="small text-muted">{billing.region} region</div>
          </div>
        </div>
        <div className="col-md-4">
          <div className="stat-card">
            <div className="label">Applications</div>
            <div className="value fs-5">
              {billing.applicationCount} / {billing.maxApplications}
            </div>
          </div>
        </div>
        <div className="col-md-4">
          <div className="stat-card">
            <div className="label">Analyses this month</div>
            <div className="value fs-5">
              {billing.analysisCountThisMonth} / {billing.maxAnalysesPerMonth}
            </div>
          </div>
        </div>
      </div>

      <SectionCard title="Subscription" className="mb-4">
        <dl className="row mb-0">
          <dt className="col-sm-4">Status</dt>
          <dd className="col-sm-8">{billing.subscriptionStatus}</dd>
          {billing.subscriptionPeriodEnd ? (
            <>
              <dt className="col-sm-4">Period end</dt>
              <dd className="col-sm-8">{new Date(billing.subscriptionPeriodEnd).toLocaleDateString()}</dd>
            </>
          ) : null}
        </dl>
        {billing.stripeEnabled ? (
          <div className="d-flex flex-wrap gap-2 mt-3">
            {billing.plan !== 'Team' && billing.plan !== 'Enterprise' ? (
              <>
                <button
                  type="button"
                  className="btn btn-primary btn-sm"
                  disabled={busy !== null}
                  onClick={() => void openCheckout(1)}
                >
                  Upgrade to Team
                </button>
                <button
                  type="button"
                  className="btn btn-outline-primary btn-sm"
                  disabled={busy !== null}
                  onClick={() => void openCheckout(2)}
                >
                  Upgrade to Enterprise
                </button>
              </>
            ) : null}
            {billing.hasActiveSubscription || billing.plan === 'Team' || billing.plan === 'Enterprise' ? (
              <button
                type="button"
                className="btn btn-outline-secondary btn-sm"
                disabled={busy !== null}
                onClick={() => void openPortal()}
              >
                Manage billing
              </button>
            ) : null}
          </div>
        ) : (
          <AlertBanner variant="neutral" className="mt-3">
            Stripe billing is not configured in this environment.
          </AlertBanner>
        )}
      </SectionCard>

      {data.isolation ? (
        <SectionCard title="Tenant isolation">
          <dl className="row mb-0">
            <dt className="col-sm-4">Mode</dt>
            <dd className="col-sm-8">{data.isolation.isolationMode}</dd>
            <dt className="col-sm-4">Schema</dt>
            <dd className="col-sm-8">{data.isolation.databaseSchemaName ?? '—'}</dd>
            <dt className="col-sm-4">Provisioned</dt>
            <dd className="col-sm-8">{data.isolation.isSchemaProvisioned ? 'Yes' : 'No'}</dd>
          </dl>
          {!data.isolation.isSchemaProvisioned ? (
            <button
              type="button"
              className="btn btn-outline-primary btn-sm mt-3"
              disabled={busy !== null}
              onClick={() => void handleProvision()}
            >
              Provision dedicated schema
            </button>
          ) : null}
        </SectionCard>
      ) : null}
    </div>
  );
}

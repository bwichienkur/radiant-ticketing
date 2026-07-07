import { FormEvent, useCallback, useEffect, useState } from 'react';
import {
  deleteTenantDeploymentEnvironment,
  getTenantDeliveryProfile,
  updateTenantDeliveryProfile,
  upsertTenantDeploymentEnvironment,
  validateTenantDeliveryProfile,
} from '../../api/spaClient';
import {
  AlertBanner,
  ErrorState,
  FormField,
  LoadingState,
  SectionCard,
  useToast,
} from '../../components/ui';
import type { DeliveryProfileValidation, TenantDeliveryProfile } from '../../types/spa';

export function AdminDeliverySection() {
  const toast = useToast();
  const [profile, setProfile] = useState<TenantDeliveryProfile | null>(null);
  const [validation, setValidation] = useState<DeliveryProfileValidation | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [envForm, setEnvForm] = useState(emptyEnvironmentForm());

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [loadedProfile, loadedValidation] = await Promise.all([
        getTenantDeliveryProfile(),
        validateTenantDeliveryProfile(),
      ]);
      setProfile(loadedProfile);
      setValidation(loadedValidation);
    } catch {
      setError('Failed to load delivery profile.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function handleSaveProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!profile) return;
    setSaving(true);
    try {
      setProfile(
        await updateTenantDeliveryProfile({
          defaultCicdProvider: profile.defaultCicdProvider,
          vaultSecretPrefix: profile.vaultSecretPrefix,
          autoImplementOnApprove: profile.autoImplementOnApprove,
          autoDeployToTest: profile.autoDeployToTest,
          requirePullRequestReview: profile.requirePullRequestReview,
          requireUatSignoff: profile.requireUatSignoff,
          requireProdChangeWindow: profile.requireProdChangeWindow,
          changeWindowNotes: profile.changeWindowNotes,
          qaVideoRetentionDays: profile.qaVideoRetentionDays,
          allowOneClickProdDeploy: profile.allowOneClickProdDeploy,
          allowOneClickRollback: profile.allowOneClickRollback,
          testDataStrategy: profile.testDataStrategy,
          allowProdToTestRefresh: profile.allowProdToTestRefresh,
        }),
      );
      setValidation(await validateTenantDeliveryProfile());
      toast.success('Delivery policies saved');
    } catch {
      toast.danger('Save failed', 'Could not update delivery profile.');
    } finally {
      setSaving(false);
    }
  }

  async function handleSaveEnvironment(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    try {
      await upsertTenantDeploymentEnvironment({
        environmentId: envForm.environmentId || null,
        name: envForm.name,
        environmentType: envForm.environmentType,
        baseUrlTemplate: envForm.baseUrlTemplate || null,
        secretReferencePrefix: envForm.secretReferencePrefix || null,
        isActive: envForm.isActive,
        sortOrder: envForm.sortOrder,
        requiresApprovalForDeploy: envForm.requiresApprovalForDeploy,
      });
      toast.success('Environment saved');
      setEnvForm(emptyEnvironmentForm());
      await reload();
    } catch {
      toast.danger('Save failed', 'Could not save environment.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDeleteEnvironment(id: string) {
    if (!window.confirm('Delete this deployment environment?')) return;
    try {
      await deleteTenantDeploymentEnvironment(id);
      toast.success('Environment deleted');
      await reload();
    } catch {
      toast.danger('Delete failed', 'Could not delete environment.');
    }
  }

  if (loading) {
    return <LoadingState label="Loading delivery automation…" />;
  }

  if (error || !profile) {
    return <ErrorState message={error ?? 'Unable to load profile.'} onRetry={() => void reload()} />;
  }

  return (
    <div aria-live="polite">
      <h2 className="eh-section-title mb-1">Delivery automation</h2>
      <p className="text-muted small mb-3">CI/CD defaults, deployment environments, and approval gates</p>

      {validation && !validation.isValid ? (
        <AlertBanner variant="warning" className="mb-3">
          Profile validation issues: {[...validation.errors, ...validation.warnings].join(' · ')}
        </AlertBanner>
      ) : null}

      <SectionCard title="Tenant policies" className="mb-4">
        <form onSubmit={(event) => void handleSaveProfile(event)} className="row g-3">
          <div className="col-md-6">
            <FormField label="Vault secret prefix" id="vault-prefix">
              <input
                id="vault-prefix"
                className="form-control"
                value={profile.vaultSecretPrefix ?? ''}
                onChange={(event) =>
                  setProfile((prev) => (prev ? { ...prev, vaultSecretPrefix: event.target.value } : prev))
                }
              />
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="QA video retention (days)" id="qa-retention">
              <input
                id="qa-retention"
                type="number"
                className="form-control"
                value={profile.qaVideoRetentionDays}
                onChange={(event) =>
                  setProfile((prev) =>
                    prev ? { ...prev, qaVideoRetentionDays: Number(event.target.value) } : prev,
                  )
                }
              />
            </FormField>
          </div>
          <div className="col-12 d-flex flex-wrap gap-3">
            {[
              ['autoImplementOnApprove', 'Auto-implement on approve'],
              ['autoDeployToTest', 'Auto-deploy to test'],
              ['requirePullRequestReview', 'Require PR review'],
              ['requireUatSignoff', 'Require UAT signoff'],
              ['requireProdChangeWindow', 'Require prod change window'],
            ].map(([key, label]) => (
              <label key={key} className="form-check">
                <input
                  className="form-check-input"
                  type="checkbox"
                  checked={profile[key as keyof TenantDeliveryProfile] as boolean}
                  onChange={(event) =>
                    setProfile((prev) => (prev ? { ...prev, [key]: event.target.checked } : prev))
                  }
                />
                {label}
              </label>
            ))}
          </div>
          <div className="col-12">
            <FormField label="Change window notes" id="change-window">
              <textarea
                id="change-window"
                className="form-control"
                rows={2}
                value={profile.changeWindowNotes ?? ''}
                onChange={(event) =>
                  setProfile((prev) => (prev ? { ...prev, changeWindowNotes: event.target.value } : prev))
                }
              />
            </FormField>
          </div>
          <div className="col-12">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving…' : 'Save policies'}
            </button>
          </div>
        </form>
      </SectionCard>

      <SectionCard title="Deployment environments" className="mb-4">
        {profile.environments.length === 0 ? (
          <p className="text-muted mb-3">No environments configured.</p>
        ) : (
          <div className="table-responsive mb-3">
            <table className="table table-enterprise mb-0">
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col">Type</th>
                  <th scope="col">Base URL</th>
                  <th scope="col">Active</th>
                  <th scope="col" />
                </tr>
              </thead>
              <tbody>
                {profile.environments.map((env) => (
                  <tr key={env.id}>
                    <td>{env.name}</td>
                    <td>{env.environmentType}</td>
                    <td className="small">{env.baseUrlTemplate ?? '—'}</td>
                    <td>{env.isActive ? 'Yes' : 'No'}</td>
                    <td className="text-end">
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-secondary me-2"
                        onClick={() =>
                          setEnvForm({
                            environmentId: env.id,
                            name: env.name,
                            environmentType: env.environmentType,
                            baseUrlTemplate: env.baseUrlTemplate ?? '',
                            secretReferencePrefix: env.secretReferencePrefix ?? '',
                            isActive: env.isActive,
                            sortOrder: env.sortOrder,
                            requiresApprovalForDeploy: env.requiresApprovalForDeploy,
                          })
                        }
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => void handleDeleteEnvironment(env.id)}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <form onSubmit={(event) => void handleSaveEnvironment(event)} className="row g-3">
          <div className="col-md-4">
            <FormField label="Name" id="env-name" required>
              <input
                id="env-name"
                className="form-control"
                value={envForm.name}
                onChange={(event) => setEnvForm((prev) => ({ ...prev, name: event.target.value }))}
                required
              />
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Base URL template" id="env-url">
              <input
                id="env-url"
                className="form-control"
                value={envForm.baseUrlTemplate}
                onChange={(event) => setEnvForm((prev) => ({ ...prev, baseUrlTemplate: event.target.value }))}
              />
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Sort order" id="env-sort">
              <input
                id="env-sort"
                type="number"
                className="form-control"
                value={envForm.sortOrder}
                onChange={(event) => setEnvForm((prev) => ({ ...prev, sortOrder: Number(event.target.value) }))}
              />
            </FormField>
          </div>
          <div className="col-12">
            <button type="submit" className="btn btn-outline-primary btn-sm" disabled={saving}>
              {envForm.environmentId ? 'Update environment' : 'Add environment'}
            </button>
          </div>
        </form>
      </SectionCard>
    </div>
  );
}

function emptyEnvironmentForm() {
  return {
    environmentId: '',
    name: '',
    environmentType: 1,
    baseUrlTemplate: '',
    secretReferencePrefix: '',
    isActive: true,
    sortOrder: 0,
    requiresApprovalForDeploy: false,
  };
}

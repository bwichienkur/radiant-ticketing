import { useCallback, useEffect, useState } from 'react';
import { listSystemSettings, updateSystemSetting } from '../../api/spaClient';
import { ErrorState, LoadingState, useToast } from '../../components/ui';
import type { SystemSetting } from '../../types/spa';

export function SettingsGeneralSection() {
  const toast = useToast();
  const [settings, setSettings] = useState<SystemSetting[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [savingId, setSavingId] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setSettings(await listSystemSettings());
    } catch {
      setError('Failed to load system settings.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function handleSave(setting: SystemSetting, value: string) {
    setSavingId(setting.id);
    try {
      await updateSystemSetting(setting.id, value);
      toast.success('Setting saved', setting.key);
      await reload();
    } catch (err) {
      toast.danger('Save failed', err instanceof Error ? err.message : 'Could not update setting.');
    } finally {
      setSavingId(null);
    }
  }

  if (loading) {
    return <LoadingState label="Loading system settings…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void reload()} />;
  }

  return (
    <div>
      <h2 className="eh-section-title mb-1">System settings</h2>
      <p className="text-muted small mb-3">Configure platform behavior and feature flags</p>

      {settings.length === 0 ? (
        <div className="card-panel p-4 text-muted">No settings configured yet.</div>
      ) : (
        <div className="card-panel table-desktop-only">
          <div className="table-responsive">
            <table className="table table-hover table-enterprise mb-0">
              <thead>
                <tr>
                  <th scope="col">Category</th>
                  <th scope="col">Key</th>
                  <th scope="col">Value</th>
                  <th scope="col">Description</th>
                </tr>
              </thead>
              <tbody>
                {settings.map((setting) => (
                  <SettingRow
                    key={setting.id}
                    setting={setting}
                    saving={savingId === setting.id}
                    onSave={(value) => void handleSave(setting, value)}
                  />
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

function SettingRow({
  setting,
  saving,
  onSave,
}: {
  setting: SystemSetting;
  saving: boolean;
  onSave: (value: string) => void;
}) {
  const [value, setValue] = useState(setting.value);

  useEffect(() => {
    setValue(setting.value);
  }, [setting.value]);

  return (
    <tr>
      <td>
        <span className="badge text-bg-light">{setting.category}</span>
      </td>
      <td>
        <code className="small">{setting.key}</code>
      </td>
      <td>
        <form
          className="d-flex flex-wrap gap-2 align-items-center"
          onSubmit={(event) => {
            event.preventDefault();
            onSave(value);
          }}
        >
          <label className="visually-hidden" htmlFor={`setting-${setting.id}`}>
            Value for {setting.key}
          </label>
          <input
            id={`setting-${setting.id}`}
            className="form-control form-control-sm"
            value={value}
            onChange={(event) => setValue(event.target.value)}
          />
          <button type="submit" className="btn btn-sm btn-primary" disabled={saving}>
            {saving ? 'Saving…' : 'Save'}
          </button>
        </form>
      </td>
      <td className="small text-muted">{setting.description ?? '—'}</td>
    </tr>
  );
}

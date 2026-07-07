import { FormEvent, useCallback, useEffect, useState } from 'react';
import {
  getNotificationPreferences,
  updateNotificationPreferences,
} from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import {
  AlertBanner,
  ErrorState,
  LoadingState,
  PageHeader,
  SectionCard,
} from '../components/ui';
import type { NotificationPreference } from '../types/spa';

export function NotificationPreferencesApp() {
  const [preferences, setPreferences] = useState<NotificationPreference[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setPreferences(await getNotificationPreferences());
    } catch {
      setError('Failed to load notification preferences.');
      setPreferences([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setStatusMessage(null);
    setError(null);

    try {
      const updated = await updateNotificationPreferences(
        preferences.map((preference) => ({
          type: preference.type,
          emailEnabled: preference.emailEnabled,
          inAppEnabled: preference.inAppEnabled,
        })),
      );
      setPreferences(updated);
      setStatusMessage('Notification preferences saved.');
    } catch {
      setError('Failed to save notification preferences.');
    } finally {
      setSaving(false);
    }
  }

  function updatePreference(
    type: number,
    field: 'emailEnabled' | 'inAppEnabled',
    value: boolean,
  ) {
    setPreferences((current) =>
      current.map((preference) =>
        preference.type === type ? { ...preference, [field]: value } : preference,
      ),
    );
  }

  if (loading) {
    return <LoadingState label="Loading notification preferences…" />;
  }

  if (error && preferences.length === 0) {
    return <ErrorState message={error} onRetry={() => void reload()} />;
  }

  return (
    <div aria-live="polite">
      <PageHeader
        title="Notification preferences"
        description="Choose how you receive alerts for approvals, analysis, and schema drift."
      />

      {statusMessage ? (
        <AlertBanner variant="success" className="mb-3">
          {statusMessage}
        </AlertBanner>
      ) : null}

      {error ? (
        <AlertBanner variant="warning" className="mb-3">
          {error}
        </AlertBanner>
      ) : null}

      <form onSubmit={(event) => void handleSubmit(event)}>
        <SectionCard title="Delivery channels">
          <div className="table-responsive">
            <table className="table table-hover table-enterprise mb-0">
              <thead>
                <tr>
                  <th scope="col">Notification</th>
                  <th scope="col" className="text-center">
                    In-app
                  </th>
                  <th scope="col" className="text-center">
                    Email
                  </th>
                </tr>
              </thead>
              <tbody>
                {preferences.map((preference) => (
                  <tr key={preference.type}>
                    <td>
                      <strong>{preference.label}</strong>
                    </td>
                    <td className="text-center">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        checked={preference.inAppEnabled}
                        onChange={(event) =>
                          updatePreference(preference.type, 'inAppEnabled', event.target.checked)
                        }
                        aria-label={`${preference.label} in-app`}
                      />
                    </td>
                    <td className="text-center">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        checked={preference.emailEnabled}
                        onChange={(event) =>
                          updatePreference(preference.type, 'emailEnabled', event.target.checked)
                        }
                        aria-label={`${preference.label} email`}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="border-top px-3 py-3 d-flex flex-wrap gap-2">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving…' : 'Save preferences'}
            </button>
            <SpaLink href="/" className="btn btn-outline-secondary">
              Back to dashboard
            </SpaLink>
          </div>
        </SectionCard>
      </form>
    </div>
  );
}

import { Fragment, useCallback, useEffect, useState } from 'react';
import {
  createWebhookSubscription,
  listWebhookDeliveries,
  listWebhookEventTypes,
  listWebhookSubscriptions,
  revokeWebhookSubscription,
} from '../../api/spaClient';
import {
  AlertBanner,
  ErrorState,
  FormField,
  LoadingState,
  SectionCard,
  useToast,
} from '../../components/ui';
import type {
  CreateWebhookSubscriptionResult,
  WebhookDeliverySummary,
  WebhookSubscriptionSummary,
} from '../../types/spa';

export function SettingsWebhooksSection() {
  const toast = useToast();
  const [subscriptions, setSubscriptions] = useState<WebhookSubscriptionSummary[]>([]);
  const [deliveries, setDeliveries] = useState<WebhookDeliverySummary[]>([]);
  const [eventTypes, setEventTypes] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [revokingId, setRevokingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [createdSecret, setCreatedSecret] = useState<CreateWebhookSubscriptionResult | null>(null);
  const [name, setName] = useState('');
  const [url, setUrl] = useState('');
  const [selectedEvents, setSelectedEvents] = useState<string[]>(['request.approved']);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [subs, deliveryList, events] = await Promise.all([
        listWebhookSubscriptions(),
        listWebhookDeliveries(),
        listWebhookEventTypes(),
      ]);
      setSubscriptions(subs);
      setDeliveries(deliveryList);
      setEventTypes(events);
      if (events.length > 0 && selectedEvents.length === 0) {
        setSelectedEvents([events[0]]);
      }
    } catch {
      setError('Failed to load webhooks.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  function toggleEvent(eventType: string) {
    setSelectedEvents((current) =>
      current.includes(eventType)
        ? current.filter((item) => item !== eventType)
        : [...current, eventType],
    );
  }

  async function handleCreate(event: React.FormEvent) {
    event.preventDefault();
    if (!name.trim() || !url.trim() || selectedEvents.length === 0) {
      return;
    }

    setSubmitting(true);
    try {
      const result = await createWebhookSubscription({
        name: name.trim(),
        url: url.trim(),
        eventTypes: selectedEvents,
      });
      setCreatedSecret(result);
      toast.success('Webhook created', result.name);
      setName('');
      setUrl('');
      await reload();
    } catch (err) {
      toast.danger('Create failed', err instanceof Error ? err.message : 'Could not create webhook.');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleRevoke(subscriptionId: string) {
    if (!window.confirm('Revoke this webhook subscription?')) {
      return;
    }

    setRevokingId(subscriptionId);
    try {
      await revokeWebhookSubscription(subscriptionId);
      toast.success('Webhook revoked');
      await reload();
    } catch {
      toast.danger('Revoke failed', 'Could not revoke webhook.');
    } finally {
      setRevokingId(null);
    }
  }

  if (loading) {
    return <LoadingState label="Loading webhooks…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void reload()} />;
  }

  return (
    <SectionCard title="Outbound webhooks">
      <p className="text-muted small mb-3">Register HTTPS endpoints to receive signed workflow events</p>

      {createdSecret ? (
        <AlertBanner variant="warning" title="Copy this signing secret now — it will not be shown again." className="mb-3">
          <pre className="mb-0 mt-2 user-select-all">{createdSecret.secret}</pre>
        </AlertBanner>
      ) : null}

      <div className="row g-4">
        <div className="col-lg-4">
          <form className="card-panel p-4" onSubmit={(event) => void handleCreate(event)}>
            <h2 className="h6 mb-3">Create subscription</h2>
            <FormField label="Name" id="webhook-name" required>
              <input
                id="webhook-name"
                className="form-control"
                value={name}
                onChange={(event) => setName(event.target.value)}
                required
                maxLength={200}
                placeholder="Zapier approval hook"
              />
            </FormField>
            <FormField label="Endpoint URL" id="webhook-url" required>
              <input
                id="webhook-url"
                className="form-control"
                value={url}
                onChange={(event) => setUrl(event.target.value)}
                required
                maxLength={2000}
                placeholder="https://hooks.example.com/enhancementhub"
              />
            </FormField>
            <fieldset className="mb-3">
              <legend className="form-label fs-6">Event types</legend>
              {eventTypes.map((eventType) => (
                <div key={eventType} className="form-check">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    id={`event-${eventType}`}
                    checked={selectedEvents.includes(eventType)}
                    onChange={() => toggleEvent(eventType)}
                  />
                  <label className="form-check-label" htmlFor={`event-${eventType}`}>
                    {eventType}
                  </label>
                </div>
              ))}
            </fieldset>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? 'Creating…' : 'Create webhook'}
            </button>
          </form>
        </div>

        <div className="col-lg-8">
          <div className="card-panel mb-4">
            <div className="card-header eh-section-title px-3 py-3">Subscriptions</div>
            {subscriptions.length === 0 ? (
              <div className="p-4 text-muted">No webhook subscriptions yet.</div>
            ) : (
              <div className="table-responsive">
                <table className="table table-enterprise mb-0">
                  <thead>
                    <tr>
                      <th scope="col">Name</th>
                      <th scope="col">URL</th>
                      <th scope="col">Events</th>
                      <th scope="col">Status</th>
                      <th scope="col">Failed</th>
                      <th scope="col" />
                    </tr>
                  </thead>
                  <tbody>
                    {subscriptions.map((subscription) => (
                      <tr key={subscription.id}>
                        <td>
                          <strong>{subscription.name}</strong>
                        </td>
                        <td className="small text-break">{subscription.url}</td>
                        <td className="small">{subscription.eventTypes}</td>
                        <td>
                          <span
                            className={`badge ${subscription.isActive ? 'text-bg-success' : 'text-bg-secondary'}`}
                          >
                            {subscription.isActive ? 'Active' : 'Revoked'}
                          </span>
                        </td>
                        <td>{subscription.failedDeliveryCount}</td>
                        <td className="text-end">
                          {subscription.isActive ? (
                            <button
                              type="button"
                              className="btn btn-sm btn-outline-danger"
                              disabled={revokingId === subscription.id}
                              onClick={() => void handleRevoke(subscription.id)}
                            >
                              Revoke
                            </button>
                          ) : null}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <div className="card-panel">
            <div className="card-header eh-section-title px-3 py-3">Recent deliveries</div>
            {deliveries.length === 0 ? (
              <div className="p-4 text-muted">No deliveries yet.</div>
            ) : (
              <div className="table-responsive">
                <table className="table table-enterprise mb-0">
                  <thead>
                    <tr>
                      <th scope="col">Subscription</th>
                      <th scope="col">Event</th>
                      <th scope="col">Status</th>
                      <th scope="col">Attempts</th>
                      <th scope="col">HTTP</th>
                      <th scope="col">When</th>
                    </tr>
                  </thead>
                  <tbody>
                    {deliveries.map((delivery) => (
                      <Fragment key={delivery.id}>
                        <tr>
                          <td>{delivery.subscriptionName}</td>
                          <td>
                            <code className="small">{delivery.eventType}</code>
                          </td>
                          <td>
                            <span
                              className={`badge ${
                                delivery.status === 'Delivered'
                                  ? 'text-bg-success'
                                  : delivery.status === 'Failed'
                                    ? 'text-bg-danger'
                                    : 'text-bg-warning'
                              }`}
                            >
                              {delivery.status}
                            </span>
                          </td>
                          <td>{delivery.attemptCount}</td>
                          <td>{delivery.httpStatusCode?.toString() ?? '—'}</td>
                          <td className="small">{new Date(delivery.createdAt).toLocaleString()}</td>
                        </tr>
                        {delivery.lastError ? (
                          <tr>
                            <td colSpan={6} className="small text-danger pb-3">
                              {delivery.lastError}
                            </td>
                          </tr>
                        ) : null}
                      </Fragment>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>
    </SectionCard>
  );
}

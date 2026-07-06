import { FormEvent, useCallback, useEffect, useState } from 'react';
import {
  createEnhancementRequest,
  createRequestFromIntakeSession,
  getCreateRequestForm,
  getEnhancementTemplate,
} from '../api/spaClient';
import { IntakeCopilotPanel, type IntakeCopilotFormDraft } from '../components/IntakeCopilotPanel';
import { SpaLink } from '../components/SpaLink';
import {
  AlertBanner,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
} from '../components/ui';
import type { EnhancementTemplateSummary } from '../types/spa';

interface CreateRequestAppProps {
  initialTemplateId?: string;
}

const PRIORITIES = ['Low', 'Medium', 'High', 'Critical'];

export function CreateRequestApp({ initialTemplateId }: CreateRequestAppProps) {
  const [templates, setTemplates] = useState<EnhancementTemplateSummary[]>([]);
  const [applications, setApplications] = useState<Array<{ id: string; name: string }>>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedTemplateId, setSelectedTemplateId] = useState(initialTemplateId ?? '');
  const [intakeSessionId, setIntakeSessionId] = useState<string | null>(null);
  const [showManualForm, setShowManualForm] = useState(Boolean(initialTemplateId));
  const [form, setForm] = useState({
    title: '',
    businessDescription: '',
    desiredOutcome: '',
    priority: 'Medium',
    targetApplicationId: '',
    requestedDueDate: '',
    department: '',
    supportingNotes: '',
  });

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      try {
        const data = await getCreateRequestForm();
        if (!cancelled) {
          setTemplates(data.templates);
          setApplications(data.applications);
        }
      } catch {
        if (!cancelled) {
          setError('Failed to load form data.');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  const applyTemplate = useCallback(async (templateId: string) => {
    setSelectedTemplateId(templateId);
    setShowManualForm(true);
    try {
      const template = await getEnhancementTemplate(templateId);
      setForm({
        title: template.title,
        businessDescription: template.businessDescription,
        desiredOutcome: template.desiredOutcome,
        priority: template.priority,
        targetApplicationId: '',
        requestedDueDate: '',
        department: '',
        supportingNotes: template.supportingNotes ?? '',
      });
    } catch {
      setError('Failed to load template.');
    }
  }, []);

  useEffect(() => {
    if (!initialTemplateId) {
      return;
    }

    void applyTemplate(initialTemplateId);
  }, [initialTemplateId, applyTemplate]);

  const applyCopilotDraft = useCallback((draft: IntakeCopilotFormDraft) => {
    setShowManualForm(true);
    setForm({
      title: draft.title,
      businessDescription: draft.businessDescription,
      desiredOutcome: draft.desiredOutcome,
      priority: draft.priority,
      targetApplicationId: draft.targetApplicationId,
      requestedDueDate: '',
      department: draft.department,
      supportingNotes: draft.supportingNotes,
    });
    if (draft.templateId) {
      setSelectedTemplateId(draft.templateId);
    }
    setError(null);
  }, []);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const payload = {
        title: form.title,
        businessDescription: form.businessDescription,
        desiredOutcome: form.desiredOutcome,
        priority: form.priority,
        targetApplicationId: form.targetApplicationId || undefined,
        requestedDueDate: form.requestedDueDate || undefined,
        department: form.department || undefined,
        supportingNotes: form.supportingNotes || undefined,
        templateId: selectedTemplateId || undefined,
      };

      const created = intakeSessionId
        ? await createRequestFromIntakeSession(intakeSessionId, payload)
        : await createEnhancementRequest(payload);
      window.location.href = `/Spa/RequestDetail/${created.id}`;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit request.');
      setSubmitting(false);
    }
  }

  if (loading) {
    return <LoadingState label="Loading form…" />;
  }

  return (
    <div aria-live="polite">
      <PageHeader
        title="Tell us what you need changed"
        description="Describe your need in everyday language. We will help shape it into a change request for review."
      />

      <IntakeCopilotPanel onApplyDraft={applyCopilotDraft} onSessionChange={setIntakeSessionId} />

      {templates.length > 0 ? (
        <section className="mb-4">
          <h2 className="h6 text-muted text-uppercase mb-2">Or start from a template</h2>
          <div className="template-card-grid" role="list" aria-label="Request templates">
            {templates.map((template) => (
              <button
                key={template.id}
                type="button"
                className={`template-card text-start ${selectedTemplateId === template.id ? 'selected' : ''}`}
                role="listitem"
                onClick={() => void applyTemplate(template.id)}
              >
                <div className="template-domain">{template.domainCategory}</div>
                <div className="fw-semibold mt-1">{template.name}</div>
                <div className="small text-muted">{template.title}</div>
              </button>
            ))}
          </div>
        </section>
      ) : null}

      <AlertBanner variant="neutral" title="What happens next:" className="mb-4">
        After you submit, we review the impact and route the request to your approver. You can track status on
        your dashboard.
      </AlertBanner>

      {error ? <ErrorState message={error} /> : null}

      {!showManualForm ? (
        <div className="card-panel p-4 mb-4">
          <p className="mb-3 text-muted">
            Prefer to fill in the form yourself? You can enter details manually after using the assistant above,
            or open the full form now.
          </p>
          <button type="button" className="btn btn-outline-secondary" onClick={() => setShowManualForm(true)}>
            Fill in the form manually
          </button>
        </div>
      ) : (
        <div className="card-panel p-4">
          <h2 className="eh-section-title mb-4">Request details</h2>
          <form onSubmit={(e) => void handleSubmit(e)} noValidate>
            <div className="row g-3">
              <div className="col-md-8">
                <FormField id="request-title" label="Title" required>
                  <input
                    id="request-title"
                    className="form-control"
                    required
                    placeholder="e.g. Track why orders are cancelled"
                    value={form.title}
                    onChange={(event) => setForm((prev) => ({ ...prev, title: event.target.value }))}
                  />
                </FormField>
              </div>
              <div className="col-md-4">
                <FormField id="request-priority" label="Priority">
                  <select
                    id="request-priority"
                    className="form-select"
                    value={form.priority}
                    onChange={(event) => setForm((prev) => ({ ...prev, priority: event.target.value }))}
                  >
                    {PRIORITIES.map((priority) => (
                      <option key={priority} value={priority}>
                        {priority}
                      </option>
                    ))}
                  </select>
                </FormField>
              </div>
              <div className="col-md-6">
                <FormField
                  id="request-application"
                  label="Which system is affected?"
                  hint="Ask IT if you are not sure which application to pick."
                >
                  <select
                    id="request-application"
                    className="form-select"
                    value={form.targetApplicationId}
                    onChange={(event) =>
                      setForm((prev) => ({ ...prev, targetApplicationId: event.target.value }))
                    }
                  >
                    <option value="">— Select —</option>
                    {applications.map((app) => (
                      <option key={app.id} value={app.id}>
                        {app.name}
                      </option>
                    ))}
                  </select>
                </FormField>
              </div>
              <div className="col-md-3">
                <FormField id="request-department" label="Department">
                  <input
                    id="request-department"
                    className="form-control"
                    value={form.department}
                    onChange={(event) => setForm((prev) => ({ ...prev, department: event.target.value }))}
                  />
                </FormField>
              </div>
              <div className="col-md-3">
                <FormField id="request-due-date" label="Requested due date">
                  <input
                    id="request-due-date"
                    type="date"
                    className="form-control"
                    value={form.requestedDueDate}
                    onChange={(event) =>
                      setForm((prev) => ({ ...prev, requestedDueDate: event.target.value }))
                    }
                  />
                </FormField>
              </div>
              <div className="col-12">
                <FormField id="request-business-description" label="What problem are you trying to solve?" required>
                  <textarea
                    id="request-business-description"
                    className="form-control"
                    rows={4}
                    required
                    maxLength={4000}
                    placeholder="Describe the business problem and why it matters today."
                    value={form.businessDescription}
                    onChange={(event) =>
                      setForm((prev) => ({ ...prev, businessDescription: event.target.value }))
                    }
                  />
                </FormField>
              </div>
              <div className="col-12">
                <FormField id="request-desired-outcome" label="What does success look like?" required>
                  <textarea
                    id="request-desired-outcome"
                    className="form-control"
                    rows={3}
                    required
                    placeholder="e.g. Managers can run a monthly report on cancellation reasons."
                    value={form.desiredOutcome}
                    onChange={(event) =>
                      setForm((prev) => ({ ...prev, desiredOutcome: event.target.value }))
                    }
                  />
                </FormField>
              </div>
              <div className="col-12">
                <FormField
                  id="request-supporting-notes"
                  label="Anything else we should know?"
                  hint="Optional context, links, or deadlines."
                >
                  <textarea
                    id="request-supporting-notes"
                    className="form-control"
                    rows={2}
                    placeholder="Optional context, links, or deadlines."
                    value={form.supportingNotes}
                    onChange={(event) =>
                      setForm((prev) => ({ ...prev, supportingNotes: event.target.value }))
                    }
                  />
                </FormField>
              </div>
            </div>
            <div className="mt-4 d-flex gap-2">
              <button type="submit" className="btn btn-primary" disabled={submitting}>
                {submitting ? 'Submitting…' : 'Submit request'}
              </button>
              <SpaLink href="/" className="btn btn-outline-secondary">
                Cancel
              </SpaLink>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

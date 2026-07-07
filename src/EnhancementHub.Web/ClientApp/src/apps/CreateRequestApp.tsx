import { FormEvent, useCallback, useEffect, useState } from 'react';
import {
  createEnhancementRequest,
  createRequestFromIntakeSession,
  getCreateRequestForm,
  getDriftRequestDraft,
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
  SegmentedControl,
} from '../components/ui';
import type { EnhancementTemplateSummary, CustomFieldDefinition, CustomFieldValueInput } from '../types/spa';

interface CreateRequestAppProps {
  initialTemplateId?: string;
  initialDriftFindingId?: string;
}

const PRIORITIES = ['Low', 'Medium', 'High', 'Critical'];

type CreateRequestMode = 'describe' | 'template' | 'manual';

export function CreateRequestApp({ initialTemplateId, initialDriftFindingId }: CreateRequestAppProps) {
  const [templates, setTemplates] = useState<EnhancementTemplateSummary[]>([]);
  const [applications, setApplications] = useState<Array<{ id: string; name: string }>>([]);
  const [customFieldDefinitions, setCustomFieldDefinitions] = useState<CustomFieldDefinition[]>([]);
  const [customFieldValues, setCustomFieldValues] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedTemplateId, setSelectedTemplateId] = useState(initialTemplateId ?? '');
  const [intakeSessionId, setIntakeSessionId] = useState<string | null>(null);
  const [mode, setMode] = useState<CreateRequestMode>(
    initialTemplateId ? 'template' : initialDriftFindingId ? 'manual' : 'describe',
  );
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
          setCustomFieldDefinitions(data.customFields ?? []);
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
    setMode('manual');
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

  useEffect(() => {
    if (!initialDriftFindingId) {
      return;
    }

    const driftFindingId = initialDriftFindingId;
    let cancelled = false;

    async function loadDriftDraft() {
      try {
        const draft = await getDriftRequestDraft(driftFindingId);
        if (cancelled) {
          return;
        }

        setMode('manual');
        setForm({
          title: draft.title,
          businessDescription: draft.businessDescription,
          desiredOutcome: draft.desiredOutcome,
          priority: draft.priority,
          targetApplicationId: draft.targetApplicationId ?? '',
          requestedDueDate: '',
          department: '',
          supportingNotes: draft.supportingNotes ?? '',
        });
        setError(null);
      } catch {
        if (!cancelled) {
          setError('Failed to load drift finding draft.');
        }
      }
    }

    void loadDriftDraft();
    return () => {
      cancelled = true;
    };
  }, [initialDriftFindingId]);

  const applyCopilotDraft = useCallback((draft: IntakeCopilotFormDraft) => {
    setMode('manual');
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

  function buildCustomFieldPayload(): CustomFieldValueInput[] {
    return customFieldDefinitions
      .map((field) => {
        const rawValue = customFieldValues[field.key] ?? '';
        switch (field.fieldType) {
          case 'Number':
            return {
              key: field.key,
              numberValue: rawValue === '' ? undefined : Number(rawValue),
            };
          case 'Date':
            return {
              key: field.key,
              dateValue: rawValue || undefined,
            };
          case 'User':
            return {
              key: field.key,
              userValueId: rawValue || undefined,
            };
          default:
            return {
              key: field.key,
              textValue: rawValue || undefined,
            };
        }
      })
      .filter((value) => {
        const field = customFieldDefinitions.find((item) => item.key === value.key);
        if (!field) {
          return false;
        }

        if (field.isRequired) {
          return true;
        }

        return (
          value.textValue !== undefined ||
          value.numberValue !== undefined ||
          value.dateValue !== undefined ||
          value.userValueId !== undefined
        );
      });
  }

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
        customFields: buildCustomFieldPayload(),
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

      <div className="mb-4">
        <SegmentedControl
          ariaLabel="Create request mode"
          value={mode}
          onChange={setMode}
          options={[
            { value: 'describe', label: 'Describe' },
            { value: 'template', label: 'Template' },
            { value: 'manual', label: 'Manual' },
          ]}
        />
      </div>

      {mode === 'describe' ? (
        <IntakeCopilotPanel onApplyDraft={applyCopilotDraft} onSessionChange={setIntakeSessionId} />
      ) : null}

      {mode === 'template' && templates.length > 0 ? (
        <section className="mb-4">
          <h2 className="h6 text-muted text-uppercase mb-2">Start from a template</h2>
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

      {mode === 'template' && templates.length === 0 ? (
        <div className="card-panel p-4 mb-4">
          <p className="text-muted mb-0">No templates are configured yet. Switch to Describe or Manual to continue.</p>
        </div>
      ) : null}

      <AlertBanner variant="neutral" title="What happens next:" className="mb-4">
        After you submit, we review the impact and route the request to your approver. You can track status on
        your dashboard.
      </AlertBanner>

      {error ? <ErrorState message={error} /> : null}

      {mode === 'manual' ? (
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
              {customFieldDefinitions.length > 0 ? (
                <div className="col-12">
                  <h3 className="h6 text-muted text-uppercase mb-3">Additional fields</h3>
                  <div className="row g-3">
                    {customFieldDefinitions.map((field) => (
                      <div key={field.id} className="col-md-6">
                        <FormField
                          id={`custom-field-${field.key}`}
                          label={field.label}
                          required={field.isRequired}
                        >
                          {field.fieldType === 'Select' ? (
                            <select
                              id={`custom-field-${field.key}`}
                              className="form-select"
                              required={field.isRequired}
                              value={customFieldValues[field.key] ?? ''}
                              onChange={(event) =>
                                setCustomFieldValues((prev) => ({
                                  ...prev,
                                  [field.key]: event.target.value,
                                }))
                              }
                            >
                              <option value="">— Select —</option>
                              {field.options.map((option) => (
                                <option key={option} value={option}>
                                  {option}
                                </option>
                              ))}
                            </select>
                          ) : field.fieldType === 'Number' ? (
                            <input
                              id={`custom-field-${field.key}`}
                              type="number"
                              className="form-control"
                              required={field.isRequired}
                              value={customFieldValues[field.key] ?? ''}
                              onChange={(event) =>
                                setCustomFieldValues((prev) => ({
                                  ...prev,
                                  [field.key]: event.target.value,
                                }))
                              }
                            />
                          ) : field.fieldType === 'Date' ? (
                            <input
                              id={`custom-field-${field.key}`}
                              type="date"
                              className="form-control"
                              required={field.isRequired}
                              value={customFieldValues[field.key] ?? ''}
                              onChange={(event) =>
                                setCustomFieldValues((prev) => ({
                                  ...prev,
                                  [field.key]: event.target.value,
                                }))
                              }
                            />
                          ) : (
                            <input
                              id={`custom-field-${field.key}`}
                              type="text"
                              className="form-control"
                              required={field.isRequired}
                              value={customFieldValues[field.key] ?? ''}
                              onChange={(event) =>
                                setCustomFieldValues((prev) => ({
                                  ...prev,
                                  [field.key]: event.target.value,
                                }))
                              }
                            />
                          )}
                        </FormField>
                      </div>
                    ))}
                  </div>
                </div>
              ) : null}
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
      ) : null}
    </div>
  );
}

import { FormEvent, useCallback, useEffect, useState } from 'react';
import {
  createEnhancementRequest,
  getCreateRequestForm,
  getEnhancementTemplate,
} from '../api/spaClient';
import { IntakeCopilotPanel, type IntakeCopilotFormDraft } from '../components/IntakeCopilotPanel';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
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
      const created = await createEnhancementRequest({
        title: form.title,
        businessDescription: form.businessDescription,
        desiredOutcome: form.desiredOutcome,
        priority: form.priority,
        targetApplicationId: form.targetApplicationId || undefined,
        requestedDueDate: form.requestedDueDate || undefined,
        department: form.department || undefined,
        supportingNotes: form.supportingNotes || undefined,
        templateId: selectedTemplateId || undefined,
      });
      window.location.href = `/Spa/RequestDetail/${created.id}`;
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit request.');
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <div aria-busy="true">
        <p className="text-muted" role="status">
          Loading form…
        </p>
        <LoadingSkeleton />
      </div>
    );
  }

  return (
    <div aria-live="polite">
      <div className="page-header">
        <h1>New Enhancement Request</h1>
        <p className="mb-0">Use intake copilot, pick a template, or fill the form directly</p>
      </div>

      <IntakeCopilotPanel onApplyDraft={applyCopilotDraft} />

      {templates.length > 0 ? (
        <div className="template-card-grid" role="list" aria-label="Enhancement templates">
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
      ) : null}

      <div className="alert alert-light border mb-4">
        <strong>What happens next:</strong> After submit, AI analyzes the request against linked repositories
        and schema. Approvers receive the package in the approval queue.
      </div>

      {error ? (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      ) : null}

      <div className="card-panel p-4">
        <form onSubmit={(e) => void handleSubmit(e)}>
          <div className="row g-3">
            <div className="col-md-8">
              <label className="form-label" htmlFor="request-title">
                Title
              </label>
              <input
                id="request-title"
                className="form-control"
                required
                value={form.title}
                onChange={(event) => setForm((prev) => ({ ...prev, title: event.target.value }))}
              />
            </div>
            <div className="col-md-4">
              <label className="form-label" htmlFor="request-priority">
                Priority
              </label>
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
            </div>
            <div className="col-md-6">
              <label className="form-label" htmlFor="request-application">
                Target Application
              </label>
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
            </div>
            <div className="col-md-3">
              <label className="form-label" htmlFor="request-department">
                Department
              </label>
              <input
                id="request-department"
                className="form-control"
                value={form.department}
                onChange={(event) => setForm((prev) => ({ ...prev, department: event.target.value }))}
              />
            </div>
            <div className="col-md-3">
              <label className="form-label" htmlFor="request-due-date">
                Requested Due Date
              </label>
              <input
                id="request-due-date"
                type="date"
                className="form-control"
                value={form.requestedDueDate}
                onChange={(event) =>
                  setForm((prev) => ({ ...prev, requestedDueDate: event.target.value }))
                }
              />
            </div>
            <div className="col-12">
              <label className="form-label" htmlFor="request-business-description">
                Business Description
              </label>
              <textarea
                id="request-business-description"
                className="form-control"
                rows={4}
                required
                maxLength={4000}
                value={form.businessDescription}
                onChange={(event) =>
                  setForm((prev) => ({ ...prev, businessDescription: event.target.value }))
                }
              />
              <div className="form-text">Describe the business problem and current pain.</div>
            </div>
            <div className="col-12">
              <label className="form-label" htmlFor="request-desired-outcome">
                Desired Outcome
              </label>
              <textarea
                id="request-desired-outcome"
                className="form-control"
                rows={3}
                required
                value={form.desiredOutcome}
                onChange={(event) =>
                  setForm((prev) => ({ ...prev, desiredOutcome: event.target.value }))
                }
              />
            </div>
            <div className="col-12">
              <label className="form-label" htmlFor="request-supporting-notes">
                Supporting Notes
              </label>
              <textarea
                id="request-supporting-notes"
                className="form-control"
                rows={2}
                value={form.supportingNotes}
                onChange={(event) =>
                  setForm((prev) => ({ ...prev, supportingNotes: event.target.value }))
                }
              />
            </div>
          </div>
          <div className="mt-4 d-flex gap-2">
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? 'Submitting…' : 'Submit Request'}
            </button>
            <a href="/" className="btn btn-outline-secondary">
              Cancel
            </a>
          </div>
        </form>
      </div>
    </div>
  );
}

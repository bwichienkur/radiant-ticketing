import { FormEvent, useCallback, useEffect, useState } from 'react';
import {
  deleteAdminCustomField,
  listAdminCustomFields,
  upsertAdminCustomField,
} from '../../api/spaClient';
import {
  AlertBanner,
  ErrorState,
  FormField,
  LoadingState,
  SectionCard,
  useToast,
} from '../../components/ui';
import type { CustomFieldDefinition } from '../../types/spa';

const FIELD_TYPES = [
  { value: 0, label: 'Text' },
  { value: 1, label: 'Number' },
  { value: 2, label: 'Date' },
  { value: 3, label: 'User' },
  { value: 4, label: 'Select' },
];

export function AdminCustomFieldsSection() {
  const toast = useToast();
  const [fields, setFields] = useState<CustomFieldDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState(emptyForm());

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setFields(await listAdminCustomFields());
    } catch {
      setError('Failed to load custom fields.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  function startEdit(field: CustomFieldDefinition) {
    setForm({
      editId: field.id,
      key: field.key,
      label: field.label,
      fieldType: Number(field.fieldType),
      isRequired: field.isRequired,
      isActive: field.isActive,
      sortOrder: field.sortOrder,
      optionsCsv: field.options.join(', '),
    });
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSaving(true);
    try {
      const options = form.optionsCsv
        .split(',')
        .map((value) => value.trim())
        .filter(Boolean);
      await upsertAdminCustomField({
        id: form.editId,
        key: form.key,
        label: form.label,
        fieldType: Number(form.fieldType),
        isRequired: form.isRequired,
        isActive: form.isActive,
        sortOrder: form.sortOrder,
        options: options.length > 0 ? options : undefined,
      });
      toast.success('Custom field saved');
      setForm(emptyForm());
      await reload();
    } catch (err) {
      toast.danger('Save failed', err instanceof Error ? err.message : 'Could not save field.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: string) {
    if (!window.confirm('Delete this custom field?')) return;
    try {
      await deleteAdminCustomField(id);
      toast.success('Custom field deleted');
      await reload();
    } catch {
      toast.danger('Delete failed', 'Could not delete custom field.');
    }
  }

  if (loading) {
    return <LoadingState label="Loading custom fields…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void reload()} />;
  }

  return (
    <div aria-live="polite">
      <h2 className="eh-section-title mb-1">Custom fields</h2>
      <p className="text-muted small mb-3">Intake form extensions for enterprise governance metadata</p>

      <SectionCard title="Existing fields" className="mb-4">
        {fields.length === 0 ? (
          <p className="text-muted mb-0">No custom fields configured.</p>
        ) : (
          <div className="table-responsive">
            <table className="table table-enterprise mb-0">
              <thead>
                <tr>
                  <th scope="col">Key</th>
                  <th scope="col">Label</th>
                  <th scope="col">Type</th>
                  <th scope="col">Required</th>
                  <th scope="col">Active</th>
                  <th scope="col" />
                </tr>
              </thead>
              <tbody>
                {fields.map((field) => (
                  <tr key={field.id}>
                    <td>
                      <code>{field.key}</code>
                    </td>
                    <td>{field.label}</td>
                    <td>{FIELD_TYPES.find((type) => type.value === field.fieldType)?.label ?? field.fieldType}</td>
                    <td>{field.isRequired ? 'Yes' : 'No'}</td>
                    <td>{field.isActive ? 'Yes' : 'No'}</td>
                    <td className="text-end">
                      <button type="button" className="btn btn-sm btn-outline-secondary me-2" onClick={() => startEdit(field)}>
                        Edit
                      </button>
                      <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => void handleDelete(field.id)}>
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </SectionCard>

      <SectionCard title={form.editId ? 'Edit field' : 'Add field'}>
        <form onSubmit={(event) => void handleSubmit(event)} className="row g-3">
          <div className="col-md-6">
            <FormField label="Key" id="cf-key" required>
              <input
                id="cf-key"
                className="form-control"
                value={form.key}
                onChange={(event) => setForm((prev) => ({ ...prev, key: event.target.value }))}
                required
              />
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Label" id="cf-label" required>
              <input
                id="cf-label"
                className="form-control"
                value={form.label}
                onChange={(event) => setForm((prev) => ({ ...prev, label: event.target.value }))}
                required
              />
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Type" id="cf-type">
              <select
                id="cf-type"
                className="form-select"
                value={form.fieldType}
                onChange={(event) => setForm((prev) => ({ ...prev, fieldType: Number(event.target.value) }))}
              >
                {FIELD_TYPES.map((type) => (
                  <option key={type.value} value={type.value}>
                    {type.label}
                  </option>
                ))}
              </select>
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Sort order" id="cf-sort">
              <input
                id="cf-sort"
                type="number"
                className="form-control"
                value={form.sortOrder}
                onChange={(event) => setForm((prev) => ({ ...prev, sortOrder: Number(event.target.value) }))}
              />
            </FormField>
          </div>
          <div className="col-md-4 d-flex align-items-end gap-3">
            <label className="form-check">
              <input
                className="form-check-input"
                type="checkbox"
                checked={form.isRequired}
                onChange={(event) => setForm((prev) => ({ ...prev, isRequired: event.target.checked }))}
              />
              Required
            </label>
            <label className="form-check">
              <input
                className="form-check-input"
                type="checkbox"
                checked={form.isActive}
                onChange={(event) => setForm((prev) => ({ ...prev, isActive: event.target.checked }))}
              />
              Active
            </label>
          </div>
          <div className="col-12">
            <FormField label="Options (comma-separated, for Select)" id="cf-options">
              <input
                id="cf-options"
                className="form-control"
                value={form.optionsCsv}
                onChange={(event) => setForm((prev) => ({ ...prev, optionsCsv: event.target.value }))}
              />
            </FormField>
          </div>
          <div className="col-12 d-flex gap-2">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving…' : 'Save field'}
            </button>
            {form.editId ? (
              <button type="button" className="btn btn-outline-secondary" onClick={() => setForm(emptyForm())}>
                Cancel edit
              </button>
            ) : null}
          </div>
        </form>
        <AlertBanner variant="neutral" className="mt-3">
          Custom fields appear on the intake form and in request detail for approvers.
        </AlertBanner>
      </SectionCard>
    </div>
  );
}

function emptyForm() {
  return {
    editId: undefined as string | undefined,
    key: '',
    label: '',
    fieldType: 0,
    isRequired: false,
    isActive: true,
    sortOrder: 0,
    optionsCsv: '',
  };
}

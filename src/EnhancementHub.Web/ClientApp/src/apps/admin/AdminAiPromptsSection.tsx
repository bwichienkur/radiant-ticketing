import { FormEvent, useCallback, useEffect, useState } from 'react';
import { listAdminAiPrompts, updateAdminAiPrompt } from '../../api/spaClient';
import {
  ErrorState,
  FormField,
  LoadingState,
  SectionCard,
  useToast,
} from '../../components/ui';
import type { AiPromptConfiguration } from '../../types/spa';

export function AdminAiPromptsSection() {
  const toast = useToast();
  const [prompts, setPrompts] = useState<AiPromptConfiguration[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [savingId, setSavingId] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setPrompts(await listAdminAiPrompts());
    } catch {
      setError('Failed to load AI prompt configurations.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  async function handleSave(prompt: AiPromptConfiguration, event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const formData = new FormData(form);
    setSavingId(prompt.id);
    try {
      await updateAdminAiPrompt(prompt.id, {
        systemPromptTemplate: String(formData.get('systemPrompt') ?? ''),
        userPromptTemplate: String(formData.get('userPrompt') ?? ''),
        isActive: formData.get('isActive') === 'on',
      });
      toast.success('Prompt saved', prompt.name);
      await reload();
    } catch {
      toast.danger('Save failed', `Could not update ${prompt.name}.`);
    } finally {
      setSavingId(null);
    }
  }

  if (loading) {
    return <LoadingState label="Loading AI prompts…" />;
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => void reload()} />;
  }

  return (
    <div aria-live="polite">
      <h2 className="eh-section-title mb-1">AI prompts</h2>
      <p className="text-muted small mb-3">System and user prompt templates for analysis workflows</p>

      {prompts.map((prompt) => (
        <SectionCard key={prompt.id} title={`${prompt.name} (${prompt.version})`} className="mb-4">
          <form onSubmit={(event) => void handleSave(prompt, event)}>
            <div className="mb-3">
              <FormField label="System prompt" id={`system-${prompt.id}`}>
                <textarea
                  id={`system-${prompt.id}`}
                  name="systemPrompt"
                  className="form-control font-monospace small"
                  rows={6}
                  defaultValue={prompt.systemPromptTemplate}
                />
              </FormField>
            </div>
            <div className="mb-3">
              <FormField label="User prompt" id={`user-${prompt.id}`}>
                <textarea
                  id={`user-${prompt.id}`}
                  name="userPrompt"
                  className="form-control font-monospace small"
                  rows={6}
                  defaultValue={prompt.userPromptTemplate}
                />
              </FormField>
            </div>
            <label className="form-check mb-3">
              <input
                className="form-check-input"
                type="checkbox"
                name="isActive"
                defaultChecked={prompt.isActive}
              />
              Active
            </label>
            <button type="submit" className="btn btn-primary btn-sm" disabled={savingId === prompt.id}>
              {savingId === prompt.id ? 'Saving…' : 'Save prompt'}
            </button>
          </form>
        </SectionCard>
      ))}
    </div>
  );
}

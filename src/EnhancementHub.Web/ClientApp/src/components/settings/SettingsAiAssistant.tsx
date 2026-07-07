import { type FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AI_SUGGESTIONS } from '../../settings/settingsCatalog';

interface SettingsAiAssistantProps {
  open: boolean;
  onClose: () => void;
}

export function SettingsAiAssistant({ open, onClose }: SettingsAiAssistantProps) {
  const navigate = useNavigate();
  const [input, setInput] = useState('');
  const [response, setResponse] = useState<string | null>(null);

  if (!open) {
    return null;
  }

  function handleSuggestion(suggestion: (typeof AI_SUGGESTIONS)[number]) {
    setInput(suggestion.prompt);
    setResponse(
      `I can help with "${suggestion.label}". This opens ${suggestion.label} where you can review and configure the relevant controls. Would you like to go there now?`,
    );
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const match = AI_SUGGESTIONS.find(
      (s) =>
        input.toLowerCase().includes(s.label.toLowerCase()) ||
        s.prompt.toLowerCase().includes(input.toLowerCase().slice(0, 12)),
    );
    if (match) {
      setResponse(`Based on your question, I recommend starting with ${match.label}. Navigate there to configure the recommended controls.`);
    } else {
      setResponse(
        'I can help you find settings, explain configuration options, and recommend security best practices. Try a suggested prompt or search for a specific area like SSO, webhooks, or compliance.',
      );
    }
  }

  return (
    <>
      <button type="button" className="eh-settings-ai-backdrop" aria-label="Close AI assistant" onClick={onClose} />
      <aside className="eh-settings-ai-panel" aria-label="Settings AI assistant">
        <header className="eh-settings-ai-panel__header">
          <div>
            <h2 className="eh-settings-ai-panel__title">Settings Assistant</h2>
            <p className="eh-settings-ai-panel__subtitle">Configure, secure, and optimize your workspace</p>
          </div>
          <button type="button" className="btn btn-sm btn-outline-secondary" onClick={onClose}>
            Close
          </button>
        </header>

        <div className="eh-settings-ai-panel__suggestions">
          {AI_SUGGESTIONS.map((suggestion) => (
            <button
              key={suggestion.label}
              type="button"
              className="eh-settings-ai-suggestion"
              onClick={() => handleSuggestion(suggestion)}
            >
              {suggestion.label}
            </button>
          ))}
        </div>

        {response ? (
          <div className="eh-settings-ai-response">
            <p>{response}</p>
            {AI_SUGGESTIONS.find((s) => response.includes(s.label)) ? (
              <button
                type="button"
                className="btn btn-sm btn-primary mt-2"
                onClick={() => {
                  const s = AI_SUGGESTIONS.find((x) => response.includes(x.label));
                  if (s) {
                    onClose();
                    navigate(s.route);
                  }
                }}
              >
                Open setting
              </button>
            ) : null}
          </div>
        ) : null}

        <form className="eh-settings-ai-panel__form" onSubmit={handleSubmit}>
          <textarea
            className="form-control eh-input"
            rows={3}
            placeholder="Ask about SSO, compliance, API keys, retention…"
            value={input}
            onChange={(e) => setInput(e.target.value)}
          />
          <button type="submit" className="btn btn-primary w-100">
            Ask assistant
          </button>
        </form>
      </aside>
    </>
  );
}

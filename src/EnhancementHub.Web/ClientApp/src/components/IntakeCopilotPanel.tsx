import { FormEvent, useEffect, useRef, useState } from 'react';
import { AlertBanner } from './ui';
import {
  attachIntakePolicyDocument,
  attachIntakePolicyUrl,
  getIntakeCopilotBudget,
  getIntakeCopilotSession,
  scoreIntakeDraft,
  sendIntakeCopilotMessage,
  startIntakeCopilotSession,
} from '../api/spaClient';
import type {
  AiBudgetStatus,
  IntakeCopilotDraft,
  IntakeCopilotMessage,
  IntakeCopilotSession,
  IntakeQualityScore,
} from '../types/spa';

export interface IntakeCopilotFormDraft {
  title: string;
  businessDescription: string;
  desiredOutcome: string;
  priority: string;
  targetApplicationId: string;
  department: string;
  supportingNotes: string;
  templateId: string;
}

interface IntakeCopilotPanelProps {
  onApplyDraft: (draft: IntakeCopilotFormDraft) => void;
  onSessionChange?: (sessionId: string | null) => void;
}

export function IntakeCopilotPanel({ onApplyDraft, onSessionChange }: IntakeCopilotPanelProps) {
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [messages, setMessages] = useState<IntakeCopilotMessage[]>([]);
  const [followUpQuestions, setFollowUpQuestions] = useState<string[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isComplete, setIsComplete] = useState(false);
  const [usedMockAi, setUsedMockAi] = useState(false);
  const [policyLabel, setPolicyLabel] = useState<string | null>(null);
  const [policyUrl, setPolicyUrl] = useState('');
  const [policyLoading, setPolicyLoading] = useState(false);
  const [budget, setBudget] = useState<AiBudgetStatus | null>(null);
  const [qualityScore, setQualityScore] = useState<IntakeQualityScore | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    onSessionChange?.(sessionId);
  }, [sessionId, onSessionChange]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, loading]);

  useEffect(() => {
    void getIntakeCopilotBudget()
      .then(setBudget)
      .catch(() => setBudget(null));
  }, []);

  async function refreshQualityScore(draft: IntakeCopilotDraft | null | undefined) {
    if (!draft) {
      setQualityScore(null);
      return;
    }

    try {
      const score = await scoreIntakeDraft({
        title: draft.title,
        businessDescription: draft.businessDescription,
        desiredOutcome: draft.desiredOutcome,
        priority: draft.priority,
        targetApplicationId: draft.targetApplicationId ?? undefined,
        department: draft.department ?? undefined,
        supportingNotes: draft.supportingNotes ?? undefined,
      });
      setQualityScore(score);
    } catch {
      setQualityScore(null);
    }
  }

  async function ensureSession() {
    if (sessionId) {
      return sessionId;
    }

    const started = await startIntakeCopilotSession();
    setSessionId(started.session.id);
    setMessages(started.session.messages);
    if (started.assistantMessage) {
      setMessages((prev) =>
        prev.length > 0
          ? prev
          : [
              {
                role: 'assistant',
                content: started.assistantMessage,
                occurredAt: new Date().toISOString(),
              },
            ],
      );
    }
    return started.session.id;
  }

  async function handleQuickDraft(event: FormEvent) {
    event.preventDefault();
    const prompt = input.trim();
    if (!prompt || loading) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const id = sessionId ?? (await startIntakeCopilotSession(prompt)).session.id;
      if (!sessionId) {
        setSessionId(id);
      }

      const response = await sendIntakeCopilotMessage(id, prompt);
      applyTurn(response);
      setInput('');
    } catch {
      setError('Failed to generate draft. Try again or fill the form manually.');
    } finally {
      setLoading(false);
    }
  }

  async function handleSendMessage(event: FormEvent) {
    event.preventDefault();
    const text = input.trim();
    if (!text || loading) {
      return;
    }

    setLoading(true);
    setError(null);
    setMessages((prev) => [
      ...prev,
      { role: 'user', content: text, occurredAt: new Date().toISOString() },
    ]);
    setInput('');

    try {
      const id = await ensureSession();
      const response = await sendIntakeCopilotMessage(id, text);
      applyTurn(response);
    } catch {
      setError('Failed to send message.');
    } finally {
      setLoading(false);
    }
  }

  function applyTurn(response: {
    session: IntakeCopilotSession;
    assistantMessage: string;
    followUpQuestions: string[];
    isComplete: boolean;
    usedMockAi: boolean;
  }) {
    setSessionId(response.session.id);
    setMessages(response.session.messages);
    setFollowUpQuestions(response.followUpQuestions);
    setIsComplete(response.isComplete);
    setUsedMockAi(response.usedMockAi);
    setPolicyLabel(response.session.policySourceLabel ?? null);

    if (response.session.draft) {
      onApplyDraft(draftToForm(response.session.draft, response.session.suggestedTemplateId));
      void refreshQualityScore(response.session.draft);
    }
  }

  function draftToForm(draft: IntakeCopilotDraft, templateId?: string | null): IntakeCopilotFormDraft {
    return {
      title: draft.title,
      businessDescription: draft.businessDescription,
      desiredOutcome: draft.desiredOutcome,
      priority: draft.priority || 'Medium',
      targetApplicationId: draft.targetApplicationId ?? '',
      department: draft.department ?? '',
      supportingNotes: draft.supportingNotes ?? '',
      templateId: templateId ?? '',
    };
  }

  async function handleQuestionClick(question: string) {
    setInput(question);
  }

  async function handlePolicyFileChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file || policyLoading) {
      return;
    }

    setPolicyLoading(true);
    setError(null);
    try {
      const id = await ensureSession();
      const response = await attachIntakePolicyDocument(id, file);
      applyTurn(response);
    } catch {
      setError('Failed to read policy document. Use PDF, TXT, or Markdown.');
    } finally {
      setPolicyLoading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  }

  async function handlePolicyUrlSubmit(event: FormEvent) {
    event.preventDefault();
    const url = policyUrl.trim();
    if (!url || policyLoading) {
      return;
    }

    setPolicyLoading(true);
    setError(null);
    try {
      const id = await ensureSession();
      const response = await attachIntakePolicyUrl(id, url);
      applyTurn(response);
      setPolicyUrl('');
    } catch {
      setError('Failed to fetch policy URL. Check the address and try again.');
    } finally {
      setPolicyLoading(false);
    }
  }

  return (
    <section
      className="card-panel p-4 mb-4 intake-copilot-panel"
      aria-label="Describe your need"
      data-tour="intake-copilot"
    >
      <div className="d-flex justify-content-between align-items-start flex-wrap gap-2 mb-2">
        <div>
          <h2 className="h5 mb-1">Describe your need</h2>
          <p className="small text-muted mb-0">
            Tell us what you want changed in everyday language. We will ask a few follow-up questions
            and draft your request — this is not a general chatbot.
          </p>
        </div>
        {budget?.enabled ? (
          <div className="small text-muted text-end" aria-live="polite">
            <div className="fw-semibold">Daily AI budget</div>
            <div>
              {budget.tokensRemaining.toLocaleString()} tokens left
              <span className="text-muted">
                {' '}
                / {budget.dailyTokenLimit.toLocaleString()}
              </span>
            </div>
          </div>
        ) : null}
      </div>

      {usedMockAi ? (
        <AlertBanner variant="warning" title="Offline draft mode" className="mb-3">
          AI provider is not configured — responses use deterministic mock drafting.
        </AlertBanner>
      ) : null}

      <div className="mb-3 p-3 border rounded bg-light-subtle">
        <div className="small fw-semibold mb-2">Have a compliance policy?</div>
        <p className="small text-muted mb-2">
          Upload a PDF or paste a public web link. We will read the policy and help draft a
          compliance-related request.
        </p>
        <p className="small text-muted mb-2">
          <strong>Privacy note:</strong> Policy text is stored only for this request and sensitive
          details are redacted before saving.
        </p>
        {policyLabel ? (
          <div className="alert alert-info py-2 small mb-2" role="status">
            Policy attached: <strong>{policyLabel}</strong>
          </div>
        ) : null}
        <div className="d-flex flex-wrap gap-2 align-items-center mb-2">
          <input
            ref={fileInputRef}
            type="file"
            id="intake-policy-file"
            className="form-control form-control-sm"
            style={{ maxWidth: '16rem' }}
            accept=".pdf,.txt,.md,.csv"
            onChange={(event) => void handlePolicyFileChange(event)}
            disabled={policyLoading || loading}
            aria-label="Upload policy document"
          />
        </div>
        <form className="d-flex flex-wrap gap-2" onSubmit={(e) => void handlePolicyUrlSubmit(e)}>
          <input
            type="url"
            className="form-control form-control-sm"
            style={{ minWidth: '14rem', flex: '1 1 14rem' }}
            placeholder="https://example.com/privacy-policy"
            value={policyUrl}
            onChange={(event) => setPolicyUrl(event.target.value)}
            disabled={policyLoading || loading}
            aria-label="Policy document URL"
          />
          <button
            type="submit"
            className="btn btn-outline-primary btn-sm"
            disabled={policyLoading || loading || !policyUrl.trim()}
          >
            Use this link
          </button>
        </form>
      </div>

      {messages.length > 0 ? (
        <div className="intake-copilot-messages mb-3" role="log" aria-live="polite">
          {messages.map((message, index) => (
            <div
              key={`${message.occurredAt}-${index}`}
              className={`intake-copilot-message intake-copilot-message-${message.role}`}
            >
              <div className="small text-muted mb-1">{message.role === 'user' ? 'You' : 'Assistant'}</div>
              <div>{message.content}</div>
            </div>
          ))}
          {loading ? (
            <div className="intake-copilot-message intake-copilot-message-assistant">
              <div className="small text-muted mb-1">Assistant</div>
              <div className="text-muted">Thinking…</div>
            </div>
          ) : null}
          <div ref={messagesEndRef} />
        </div>
      ) : null}

      {followUpQuestions.length > 0 && !isComplete ? (
        <div className="mb-3">
          <div className="small fw-semibold mb-2">Quick replies</div>
          <div className="d-flex flex-wrap gap-2">
            {followUpQuestions.map((question) => (
              <button
                key={question}
                type="button"
                className="btn btn-sm btn-outline-primary"
                onClick={() => void handleQuestionClick(question)}
              >
                {question}
              </button>
            ))}
          </div>
        </div>
      ) : null}

      {isComplete ? (
        <div className="alert alert-success py-2 small mb-3" role="status">
          Your draft is ready — review the details below and submit when you are happy with them.
        </div>
      ) : null}

      {qualityScore ? (
        <AlertBanner
          variant={qualityScore.readyToSubmit ? 'success' : 'warning'}
          title={`Intake quality: ${qualityScore.score}/100`}
          className="mb-3"
        >
          {qualityScore.readyToSubmit ? (
            <span>Your draft has the core fields needed to submit.</span>
          ) : (
            <span>
              {qualityScore.missingFields.length > 0
                ? `Missing: ${qualityScore.missingFields.join(', ')}. `
                : ''}
              {qualityScore.suggestions[0] ?? 'Add more detail before submitting.'}
            </span>
          )}
        </AlertBanner>
      ) : null}

      {error ? (
        <div className="alert alert-danger py-2 small" role="alert">
          {error}
        </div>
      ) : null}

      <form onSubmit={(e) => void (messages.length === 0 ? handleQuickDraft(e) : handleSendMessage(e))}>
        <label className="form-label small" htmlFor="intake-copilot-input">
          {messages.length === 0 ? 'What do you need changed?' : 'Your reply'}
        </label>
        <textarea
          id="intake-copilot-input"
          className="form-control mb-2"
          rows={3}
          placeholder="e.g. We need to capture order cancellation reasons for compliance reporting in the commerce platform"
          value={input}
          onChange={(event) => setInput(event.target.value)}
          disabled={loading}
        />
        <div className="d-flex gap-2 flex-wrap">
          <button type="submit" className="btn btn-primary btn-sm" disabled={loading || !input.trim()}>
            {messages.length === 0 ? 'Start draft' : 'Send reply'}
          </button>
          {sessionId ? (
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              disabled={loading}
              onClick={() => void getIntakeCopilotSession(sessionId).then((s) => {
                if (s.draft) {
                  onApplyDraft(draftToForm(s.draft, s.suggestedTemplateId));
                  void refreshQualityScore(s.draft);
                }
              })}
            >
              Refresh form from draft
            </button>
          ) : null}
        </div>
      </form>
    </section>
  );
}

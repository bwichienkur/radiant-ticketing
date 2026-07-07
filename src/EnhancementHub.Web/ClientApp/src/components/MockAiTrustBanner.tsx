import { useEffect, useState } from 'react';
import { getPlatformRuntimeStatus } from '../api/spaClient';
import { AlertBanner } from './ui';

const DISMISS_KEY = 'eh-mock-ai-banner-dismissed';

export function MockAiTrustBanner() {
  const [showBanner, setShowBanner] = useState(false);
  const [dismissed, setDismissed] = useState(() => localStorage.getItem(DISMISS_KEY) === 'true');

  useEffect(() => {
    void getPlatformRuntimeStatus()
      .then((status) => {
        setShowBanner(status.usesSimulatedBackends);
      })
      .catch(() => {
        setShowBanner(false);
      });
  }, []);

  function dismiss() {
    localStorage.setItem(DISMISS_KEY, 'true');
    setDismissed(true);
  }

  if (!showBanner || dismissed) {
    return null;
  }

  return (
    <div className="mock-ai-trust-banner compact">
      <AlertBanner variant="warning" title="Simulated AI mode" className="mb-3">
        AI analysis and intake use deterministic mock output. Configure OpenAI or Azure OpenAI before buyer
        demos or production.
      </AlertBanner>
      <button
        type="button"
        className="mock-ai-trust-dismiss"
        aria-label="Dismiss simulated AI notice"
        onClick={dismiss}
      >
        ×
      </button>
    </div>
  );
}

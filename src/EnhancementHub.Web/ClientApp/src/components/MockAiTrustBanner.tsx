import { useEffect, useState } from 'react';
import { getPlatformRuntimeStatus } from '../api/spaClient';
import { AlertBanner } from './ui';

export function MockAiTrustBanner() {
  const [showBanner, setShowBanner] = useState(false);

  useEffect(() => {
    void getPlatformRuntimeStatus()
      .then((status) => {
        setShowBanner(status.usesSimulatedBackends);
      })
      .catch(() => {
        setShowBanner(false);
      });
  }, []);

  if (!showBanner) {
    return null;
  }

  return (
    <AlertBanner
      variant="warning"
      title="Simulated AI mode"
      className="mb-3"
    >
      AI analysis, intake copilot, and related features are using deterministic mock output. Configure
      OpenAI or Azure OpenAI before buyer demos or production use.
    </AlertBanner>
  );
}

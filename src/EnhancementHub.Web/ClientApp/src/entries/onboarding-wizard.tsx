import { createRoot } from 'react-dom/client';
import { OnboardingWizardApp } from '../apps/OnboardingWizardApp';

const mount = document.getElementById('spa-onboarding-wizard-root');
if (mount) {
  const sessionId = mount.dataset.sessionId;
  createRoot(mount).render(<OnboardingWizardApp initialSessionId={sessionId || undefined} />);
}

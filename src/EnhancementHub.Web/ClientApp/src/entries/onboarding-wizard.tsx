import { createRoot } from 'react-dom/client';
import { OnboardingWizardApp } from '../apps/OnboardingWizardApp';
import { SpUiRoot } from '../components/SpUiRoot';

const mount = document.getElementById('spa-onboarding-wizard-root');
if (mount) {
  const sessionId = mount.dataset.sessionId;
  createRoot(mount).render(
    <SpUiRoot>
      <OnboardingWizardApp initialSessionId={sessionId || undefined} />
    </SpUiRoot>,
  );
}

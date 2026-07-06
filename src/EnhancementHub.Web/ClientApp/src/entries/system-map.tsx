import { createRoot } from 'react-dom/client';
import { SystemMapApp } from '../apps/SystemMapApp';
import { SpUiRoot } from '../components/SpUiRoot';

const mount = document.getElementById('spa-system-map-root');
if (mount) {
  const applicationId = mount.dataset.applicationId;
  createRoot(mount).render(
    <SpUiRoot>
      <SystemMapApp initialApplicationId={applicationId} />
    </SpUiRoot>,
  );
}

import { createRoot } from 'react-dom/client';
import { DashboardApp } from '../apps/DashboardApp';
import { SpUiRoot } from '../components/SpUiRoot';

const mount = document.getElementById('spa-dashboard-root');
if (mount) {
  createRoot(mount).render(
    <SpUiRoot>
      <DashboardApp />
    </SpUiRoot>,
  );
}

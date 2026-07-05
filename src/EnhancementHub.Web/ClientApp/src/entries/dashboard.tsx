import { createRoot } from 'react-dom/client';
import { DashboardApp } from '../apps/DashboardApp';

const mount = document.getElementById('spa-dashboard-root');
if (mount) {
  createRoot(mount).render(<DashboardApp />);
}

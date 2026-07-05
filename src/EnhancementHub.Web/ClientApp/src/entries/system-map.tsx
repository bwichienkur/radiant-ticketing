import { createRoot } from 'react-dom/client';
import { SystemMapApp } from '../apps/SystemMapApp';

const mount = document.getElementById('spa-system-map-root');
if (mount) {
  const applicationId = mount.dataset.applicationId;
  createRoot(mount).render(<SystemMapApp initialApplicationId={applicationId} />);
}

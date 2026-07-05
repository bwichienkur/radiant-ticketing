import { createRoot } from 'react-dom/client';
import { RequestDetailApp } from '../apps/RequestDetailApp';

const mount = document.getElementById('spa-request-detail-root');
if (mount) {
  const requestId = mount.dataset.requestId;
  if (requestId) {
    createRoot(mount).render(<RequestDetailApp requestId={requestId} />);
  }
}

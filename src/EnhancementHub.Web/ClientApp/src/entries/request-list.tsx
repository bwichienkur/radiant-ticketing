import { createRoot } from 'react-dom/client';
import { RequestListApp } from '../apps/RequestListApp';

const mount = document.getElementById('spa-request-list-root');
if (mount) {
  createRoot(mount).render(<RequestListApp />);
}

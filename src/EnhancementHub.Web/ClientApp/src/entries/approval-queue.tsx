import { createRoot } from 'react-dom/client';
import { ApprovalQueueApp } from '../apps/ApprovalQueueApp';

const mount = document.getElementById('spa-approval-queue-root');
if (mount) {
  const requestId = mount.dataset.requestId;
  createRoot(mount).render(<ApprovalQueueApp initialRequestId={requestId || undefined} />);
}

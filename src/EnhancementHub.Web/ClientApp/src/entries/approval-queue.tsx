import { createRoot } from 'react-dom/client';
import { ApprovalQueueApp } from '../apps/ApprovalQueueApp';
import { SpUiRoot } from '../components/SpUiRoot';

const mount = document.getElementById('spa-approval-queue-root');
if (mount) {
  const requestId = mount.dataset.requestId;
  createRoot(mount).render(
    <SpUiRoot>
      <ApprovalQueueApp initialRequestId={requestId || undefined} />
    </SpUiRoot>,
  );
}

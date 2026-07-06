import { createRoot } from 'react-dom/client';
import { RequestDetailApp } from '../apps/RequestDetailApp';
import { SpUiRoot } from '../components/SpUiRoot';

const mount = document.getElementById('spa-request-detail-root');
if (mount) {
  const requestId = mount.dataset.requestId;
  if (requestId) {
    createRoot(mount).render(
      <SpUiRoot>
        <RequestDetailApp requestId={requestId} />
      </SpUiRoot>,
    );
  }
}

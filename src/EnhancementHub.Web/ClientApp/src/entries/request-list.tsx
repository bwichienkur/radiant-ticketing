import { createRoot } from 'react-dom/client';
import { RequestListApp } from '../apps/RequestListApp';
import { SpUiRoot } from '../components/SpUiRoot';

const mount = document.getElementById('spa-request-list-root');
if (mount) {
  const isApprover = mount.dataset.isApprover === 'true';
  createRoot(mount).render(
    <SpUiRoot>
      <RequestListApp isApprover={isApprover} />
    </SpUiRoot>,
  );
}

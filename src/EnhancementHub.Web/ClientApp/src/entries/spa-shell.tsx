import { createRoot } from 'react-dom/client';
import { SpaShell } from '../components/SpaShell';
import { SpUiRoot } from '../components/SpUiRoot';

const mount = document.getElementById('spa-root');
if (mount) {
  createRoot(mount).render(
    <SpUiRoot>
      <SpaShell />
    </SpUiRoot>,
  );
}

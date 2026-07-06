import { createRoot } from 'react-dom/client';
import { CreateRequestApp } from '../apps/CreateRequestApp';
import { SpUiRoot } from '../components/SpUiRoot';

const mount = document.getElementById('spa-create-request-root');
if (mount) {
  const templateId = mount.dataset.templateId;
  createRoot(mount).render(
    <SpUiRoot>
      <CreateRequestApp initialTemplateId={templateId || undefined} />
    </SpUiRoot>,
  );
}

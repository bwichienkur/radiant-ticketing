import { createRoot } from 'react-dom/client';
import { CreateRequestApp } from '../apps/CreateRequestApp';

const mount = document.getElementById('spa-create-request-root');
if (mount) {
  const templateId = mount.dataset.templateId;
  createRoot(mount).render(
    <CreateRequestApp initialTemplateId={templateId || undefined} />,
  );
}

import { Navigate, Route, Routes } from 'react-router-dom';
import { readSpaContext } from '../spaRoutes';
import { SettingsNav } from '../components/settings/SettingsNav';
import { ErrorState, PageHeader } from '../components/ui';
import { SettingsGeneralSection } from './settings/SettingsGeneralSection';
import { SettingsAuthenticationSection } from './settings/SettingsAuthenticationSection';
import { SettingsApiKeysSection } from './settings/SettingsApiKeysSection';
import { SettingsTeamsSection } from './settings/SettingsTeamsSection';
import { SettingsWebhooksSection } from './settings/SettingsWebhooksSection';

export function SettingsApp() {
  const { isAdmin } = readSpaContext();

  if (!isAdmin) {
    return (
      <ErrorState message="Administrator access is required to view settings." />
    );
  }

  return (
    <div>
      <PageHeader
        title="Administration"
        description="Platform configuration, identity, integrations, and teams"
      />
      <div className="row">
        <SettingsNav />
        <div className="col-lg-9">
          <Routes>
            <Route index element={<Navigate to="General" replace />} />
            <Route path="General" element={<SettingsGeneralSection />} />
            <Route path="Authentication" element={<SettingsAuthenticationSection />} />
            <Route path="ApiKeys" element={<SettingsApiKeysSection />} />
            <Route path="Teams" element={<SettingsTeamsSection />} />
            <Route path="Webhooks" element={<SettingsWebhooksSection />} />
            <Route path="*" element={<Navigate to="General" replace />} />
          </Routes>
        </div>
      </div>
    </div>
  );
}

import { Navigate, Route, Routes } from 'react-router-dom';
import { readSpaContext } from '../spaRoutes';
import { ErrorState } from '../components/ui';
import { SettingsCommandCenter } from '../components/settings/SettingsCommandCenter';
import { SettingsHub, SettingsCategoryLanding } from '../components/settings/SettingsHub';
import { SettingsSectionPage } from '../components/settings/SettingsSectionPage';
import { useSettingsUi } from '../components/settings/SettingsUiContext';
import { SettingsGeneralSection } from './settings/SettingsGeneralSection';
import { SettingsAuthenticationSection } from './settings/SettingsAuthenticationSection';
import { SettingsApiKeysSection } from './settings/SettingsApiKeysSection';
import { SettingsTeamsSection } from './settings/SettingsTeamsSection';
import { SettingsWebhooksSection } from './settings/SettingsWebhooksSection';
import { SettingsBrandingSection } from './settings/SettingsBrandingSection';

function SettingsHubRoute() {
  const { openAi } = useSettingsUi();
  return <SettingsHub onOpenAi={openAi} />;
}

function SettingsCategoryRoute() {
  const { openAi } = useSettingsUi();
  return <SettingsCategoryLanding onOpenAi={openAi} />;
}

export function SettingsApp() {
  const { isAdmin } = readSpaContext();

  if (!isAdmin) {
    return <ErrorState message="Administrator access is required to view settings." />;
  }

  return (
    <div className="eh-settings">
      <SettingsCommandCenter>
        <Routes>
          <Route index element={<SettingsHubRoute />} />
          <Route path="Category/:categoryId" element={<SettingsCategoryRoute />} />
          <Route
            path="General"
            element={
              <SettingsSectionPage sectionId="general">
                <SettingsGeneralSection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="Authentication"
            element={
              <SettingsSectionPage sectionId="authentication">
                <SettingsAuthenticationSection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="ApiKeys"
            element={
              <SettingsSectionPage sectionId="api-keys">
                <SettingsApiKeysSection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="Teams"
            element={
              <SettingsSectionPage sectionId="teams">
                <SettingsTeamsSection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="Webhooks"
            element={
              <SettingsSectionPage sectionId="webhooks">
                <SettingsWebhooksSection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="Branding"
            element={
              <SettingsSectionPage sectionId="branding">
                <SettingsBrandingSection />
              </SettingsSectionPage>
            }
          />
          <Route path="*" element={<Navigate to="/Spa/Settings" replace />} />
        </Routes>
      </SettingsCommandCenter>
    </div>
  );
}

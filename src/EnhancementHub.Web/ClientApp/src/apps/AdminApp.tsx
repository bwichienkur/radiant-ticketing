import { Navigate, Route, Routes } from 'react-router-dom';
import { readSpaContext } from '../spaRoutes';
import { ErrorState } from '../components/ui';
import { SettingsCommandCenter } from '../components/settings/SettingsCommandCenter';
import { SettingsSectionPage } from '../components/settings/SettingsSectionPage';
import { AdminJobsSection } from './admin/AdminJobsSection';
import { AdminComplianceSection } from './admin/AdminComplianceSection';
import { AdminCustomFieldsSection } from './admin/AdminCustomFieldsSection';
import { AdminTenancySection } from './admin/AdminTenancySection';
import { AdminObservabilitySection } from './admin/AdminObservabilitySection';
import { AdminDataScalingSection } from './admin/AdminDataScalingSection';
import { AdminRetentionSection } from './admin/AdminRetentionSection';
import { AdminDeliverySection } from './admin/AdminDeliverySection';
import { AdminAiPromptsSection } from './admin/AdminAiPromptsSection';

export function AdminApp() {
  const { isAdmin } = readSpaContext();

  if (!isAdmin) {
    return <ErrorState message="Administrator access is required to view admin pages." />;
  }

  return (
    <div className="eh-admin eh-settings">
      <SettingsCommandCenter>
        <Routes>
          <Route index element={<Navigate to="Jobs" replace />} />
          <Route
            path="Jobs"
            element={
              <SettingsSectionPage sectionId="jobs">
                <AdminJobsSection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="Compliance"
            element={
              <SettingsSectionPage sectionId="compliance">
                <AdminComplianceSection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="CustomFields"
            element={
              <SettingsSectionPage sectionId="custom-fields">
                <AdminCustomFieldsSection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="Tenancy"
            element={
              <SettingsSectionPage sectionId="tenancy">
                <AdminTenancySection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="Observability"
            element={
              <SettingsSectionPage sectionId="observability">
                <AdminObservabilitySection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="DataScaling"
            element={
              <SettingsSectionPage sectionId="data-scaling">
                <AdminDataScalingSection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="Retention"
            element={
              <SettingsSectionPage sectionId="retention">
                <AdminRetentionSection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="Delivery"
            element={
              <SettingsSectionPage sectionId="delivery">
                <AdminDeliverySection />
              </SettingsSectionPage>
            }
          />
          <Route
            path="AiPrompts"
            element={
              <SettingsSectionPage sectionId="ai-prompts">
                <AdminAiPromptsSection />
              </SettingsSectionPage>
            }
          />
          <Route path="*" element={<Navigate to="Jobs" replace />} />
        </Routes>
      </SettingsCommandCenter>
    </div>
  );
}

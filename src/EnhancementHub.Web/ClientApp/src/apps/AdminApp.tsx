import { Navigate, Route, Routes } from 'react-router-dom';
import { readSpaContext } from '../spaRoutes';
import { AdminNav } from '../components/admin/AdminNav';
import { ErrorState, PageHeader } from '../components/ui';
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
    <div className="eh-admin">
      <PageHeader
        title="Platform administration"
        description="Jobs, compliance, tenancy, delivery automation, and operational controls"
      />
      <div className="row">
        <AdminNav />
        <div className="col-lg-9">
          <Routes>
            <Route index element={<Navigate to="Jobs" replace />} />
            <Route path="Jobs" element={<AdminJobsSection />} />
            <Route path="Compliance" element={<AdminComplianceSection />} />
            <Route path="CustomFields" element={<AdminCustomFieldsSection />} />
            <Route path="Tenancy" element={<AdminTenancySection />} />
            <Route path="Observability" element={<AdminObservabilitySection />} />
            <Route path="DataScaling" element={<AdminDataScalingSection />} />
            <Route path="Retention" element={<AdminRetentionSection />} />
            <Route path="Delivery" element={<AdminDeliverySection />} />
            <Route path="AiPrompts" element={<AdminAiPromptsSection />} />
            <Route path="*" element={<Navigate to="Jobs" replace />} />
          </Routes>
        </div>
      </div>
    </div>
  );
}

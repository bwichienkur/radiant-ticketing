import { useEffect } from 'react';
import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
  useLocation,
  useNavigate,
  useParams,
  useSearchParams,
} from 'react-router-dom';
import { DashboardApp } from '../apps/DashboardApp';
import { RequestListApp } from '../apps/RequestListApp';
import { CreateRequestApp } from '../apps/CreateRequestApp';
import { RequestDetailApp } from '../apps/RequestDetailApp';
import { ApprovalQueueApp } from '../apps/ApprovalQueueApp';
import { OnboardingWizardApp } from '../apps/OnboardingWizardApp';
import { SystemMapApp } from '../apps/SystemMapApp';
import { ApplicationsApp } from '../apps/ApplicationsApp';
import { SchemaDriftApp } from '../apps/SchemaDriftApp';
import { RepositoriesApp } from '../apps/RepositoriesApp';
import { AuditApp } from '../apps/AuditApp';
import { SearchApp } from '../apps/SearchApp';
import { readSpaContext } from '../spaRoutes';

function RequestListRoute() {
  const { isApprover } = readSpaContext();
  return <RequestListApp isApprover={isApprover} />;
}

function RequestDetailRoute() {
  const { id } = useParams<{ id: string }>();
  if (!id) {
    return <Navigate to="/Spa/RequestList" replace />;
  }

  return <RequestDetailApp requestId={id} />;
}

function ApprovalQueueRoute() {
  const { id } = useParams<{ id: string }>();
  return <ApprovalQueueApp initialRequestId={id} />;
}

function CreateRequestRoute() {
  const [search] = useSearchParams();
  const templateId = search.get('templateId') ?? undefined;
  const driftFindingId = search.get('driftFindingId') ?? undefined;
  return (
    <CreateRequestApp initialTemplateId={templateId} initialDriftFindingId={driftFindingId} />
  );
}

function OnboardingWizardRoute() {
  const { sessionId } = useParams<{ sessionId: string }>();
  return <OnboardingWizardApp initialSessionId={sessionId} />;
}

function SystemMapRoute() {
  const [search] = useSearchParams();
  const applicationId = search.get('ApplicationId') ?? search.get('applicationId') ?? undefined;
  return <SystemMapApp initialApplicationId={applicationId ?? undefined} />;
}

function UnknownSpaRoute() {
  return (
    <div className="card-panel p-4" role="status">
      <p className="mb-0 text-muted">This page is not part of the React shell. Use the sidebar to navigate.</p>
    </div>
  );
}

function SpaNavigationBridge() {
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    function onNavigate(event: Event) {
      const detail = (event as CustomEvent<{ path: string }>).detail;
      if (detail?.path) {
        navigate(detail.path);
      }
    }

    window.addEventListener('eh-spa-navigate', onNavigate);
    return () => window.removeEventListener('eh-spa-navigate', onNavigate);
  }, [navigate]);

  useEffect(() => {
    const ehUx = (window as Window & { EhUx?: { updateSidebarActive?: (path: string) => void } }).EhUx;
    ehUx?.updateSidebarActive?.(location.pathname);
  }, [location.pathname, location.search]);

  return null;
}

export function SpaShell() {
  return (
    <BrowserRouter>
      <SpaNavigationBridge />
      <Routes>
        <Route path="/" element={<DashboardApp />} />
        <Route path="/Index" element={<DashboardApp />} />
        <Route path="/Spa/RequestList" element={<RequestListRoute />} />
        <Route path="/Spa/CreateRequest" element={<CreateRequestRoute />} />
        <Route path="/Spa/RequestDetail/:id" element={<RequestDetailRoute />} />
        <Route path="/Spa/ApprovalQueue" element={<ApprovalQueueRoute />} />
        <Route path="/Spa/ApprovalQueue/:id" element={<ApprovalQueueRoute />} />
        <Route path="/Spa/OnboardingWizard" element={<OnboardingWizardRoute />} />
        <Route path="/Spa/OnboardingWizard/:sessionId" element={<OnboardingWizardRoute />} />
        <Route path="/Spa/SystemMap" element={<SystemMapRoute />} />
        <Route path="/Spa/Applications" element={<ApplicationsApp />} />
        <Route path="/Spa/SchemaDrift" element={<SchemaDriftApp />} />
        <Route path="/Spa/Repositories" element={<RepositoriesApp />} />
        <Route path="/Spa/Audit" element={<AuditApp />} />
        <Route path="/Spa/Search" element={<SearchApp />} />
        <Route path="*" element={<UnknownSpaRoute />} />
      </Routes>
    </BrowserRouter>
  );
}

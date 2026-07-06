import { lazy, Suspense, useEffect } from 'react';
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
import { LoadingState } from './ui';
import { readSpaContext } from '../spaRoutes';

const DashboardApp = lazy(() =>
  import('../apps/DashboardApp').then((module) => ({ default: module.DashboardApp })),
);
const RequestListApp = lazy(() =>
  import('../apps/RequestListApp').then((module) => ({ default: module.RequestListApp })),
);
const CreateRequestApp = lazy(() =>
  import('../apps/CreateRequestApp').then((module) => ({ default: module.CreateRequestApp })),
);
const RequestDetailApp = lazy(() =>
  import('../apps/RequestDetailApp').then((module) => ({ default: module.RequestDetailApp })),
);
const ApprovalQueueApp = lazy(() =>
  import('../apps/ApprovalQueueApp').then((module) => ({ default: module.ApprovalQueueApp })),
);
const OnboardingWizardApp = lazy(() =>
  import('../apps/OnboardingWizardApp').then((module) => ({ default: module.OnboardingWizardApp })),
);
const SystemMapApp = lazy(() =>
  import('../apps/SystemMapApp').then((module) => ({ default: module.SystemMapApp })),
);

function RouteFallback() {
  return <LoadingState label="Loading view…" />;
}

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
      <Suspense fallback={<RouteFallback />}>
        <Routes>
          <Route path="/" element={<DashboardApp />} />
          <Route path="/Index" element={<DashboardApp />} />
          <Route path="/Spa/RequestList" element={<RequestListRoute />} />
          <Route path="/Spa/CreateRequest" element={<CreateRequestApp />} />
          <Route path="/Spa/RequestDetail/:id" element={<RequestDetailRoute />} />
          <Route path="/Spa/ApprovalQueue" element={<ApprovalQueueRoute />} />
          <Route path="/Spa/ApprovalQueue/:id" element={<ApprovalQueueRoute />} />
          <Route path="/Spa/OnboardingWizard" element={<OnboardingWizardRoute />} />
          <Route path="/Spa/OnboardingWizard/:sessionId" element={<OnboardingWizardRoute />} />
          <Route path="/Spa/SystemMap" element={<SystemMapRoute />} />
          <Route path="*" element={<UnknownSpaRoute />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  );
}

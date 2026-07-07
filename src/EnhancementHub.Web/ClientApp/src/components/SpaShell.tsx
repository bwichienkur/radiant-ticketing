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
import { ApplicationDetailApp } from '../apps/ApplicationDetailApp';
import { NotificationPreferencesApp } from '../apps/NotificationPreferencesApp';
import { SchemaDriftApp } from '../apps/SchemaDriftApp';
import { RepositoriesApp } from '../apps/RepositoriesApp';
import { AuditApp } from '../apps/AuditApp';
import { SearchApp } from '../apps/SearchApp';
import { DatabaseConnectionsApp } from '../apps/DatabaseConnectionsApp';
import { DatabaseConnectionRegisterApp } from '../apps/DatabaseConnectionRegisterApp';
import { DatabaseConnectionDetailsApp } from '../apps/DatabaseConnectionDetailsApp';
import { DatabaseConnectionErdApp } from '../apps/DatabaseConnectionErdApp';
import { DocumentationExportApp } from '../apps/DocumentationExportApp';
import { RefactorAnalyzeApp } from '../apps/RefactorAnalyzeApp';
import { RefactorPlansApp } from '../apps/RefactorPlansApp';
import { SettingsApp } from '../apps/SettingsApp';
import { InsightsApp } from '../apps/InsightsApp';
import { PortfolioHealthApp } from '../apps/PortfolioHealthApp';
import { AdminApp } from '../apps/AdminApp';
import { PortfolioHubApp } from '../apps/PortfolioHubApp';
import { FeedbackWidget } from './FeedbackWidget';
import { resolveSpaPageMeta } from '../spaPageMeta';
import { MockAiTrustBanner } from './MockAiTrustBanner';
import { CommandPalette } from './CommandPalette';
import { ThemePreferenceSelector } from './ThemePreferenceSelector';
import { getUserAppearance } from '../api/spaClient';
import { readSpaContext } from '../spaRoutes';
import {
  applyTenantBranding,
  applyThemePreference,
  readStoredThemePreference,
  type ThemePreference,
} from '../theme';

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

function ApplicationDetailRoute() {
  const { id } = useParams<{ id: string }>();
  if (!id) {
    return <Navigate to="/Spa/Applications" replace />;
  }

  return <ApplicationDetailApp applicationId={id} />;
}

function DatabaseConnectionDetailRoute() {
  const { id } = useParams<{ id: string }>();
  if (!id) {
    return <Navigate to="/Spa/DatabaseConnections" replace />;
  }

  return <DatabaseConnectionDetailsApp connectionId={id} />;
}

function DatabaseConnectionErdRoute() {
  const { id } = useParams<{ id: string }>();
  if (!id) {
    return <Navigate to="/Spa/DatabaseConnections" replace />;
  }

  return <DatabaseConnectionErdApp connectionId={id} />;
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

  useEffect(() => {
    const liveRegion = document.getElementById('eh-spa-live-region');
    const meta = resolveSpaPageMeta(location.pathname);
    const pageTitle = document.querySelector('.page-header-title, .eh-section-title, h1')?.textContent?.trim();
    const announcement = pageTitle && pageTitle.length > 0
      ? `Navigated to ${pageTitle}`
      : `Navigated to ${meta.breadcrumb}`;

    if (liveRegion) {
      liveRegion.textContent = announcement;
    }

    document.title = `${pageTitle || meta.title} - EnhancementHub`;

    const sectionEl = document.getElementById('eh-topbar-crumb-section');
    const currentEl = document.getElementById('eh-topbar-crumb-current');
    if (sectionEl) {
      sectionEl.textContent = meta.section ?? 'Home';
    }
    if (currentEl) {
      currentEl.textContent = pageTitle || meta.breadcrumb;
    }
  }, [location.pathname, location.search]);

  return null;
}

function SpaAppearanceBootstrap() {
  useEffect(() => {
    void getUserAppearance()
      .then((appearance) => {
        const pref = appearance.themePreference as ThemePreference;
        if (pref === 'System' || pref === 'Light' || pref === 'Dark') {
          applyThemePreference(pref);
        }

        applyTenantBranding(
          appearance.branding.accentColor,
          appearance.branding.productName,
          appearance.branding.logoUrl,
        );
      })
      .catch(() => {
        applyThemePreference(readStoredThemePreference());
      });
  }, []);

  return null;
}

export function SpaShell() {
  return (
    <BrowserRouter>
      <SpaNavigationBridge />
      <SpaAppearanceBootstrap />
      <MockAiTrustBanner />
      <ThemePreferenceSelector />
      <CommandPalette />
      <FeedbackWidget />
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
        <Route path="/Spa/Applications/:id" element={<ApplicationDetailRoute />} />
        <Route path="/Spa/Account/Notifications" element={<NotificationPreferencesApp />} />
        <Route path="/Spa/SchemaDrift" element={<SchemaDriftApp />} />
        <Route path="/Spa/Repositories" element={<RepositoriesApp />} />
        <Route path="/Spa/Audit" element={<AuditApp />} />
        <Route path="/Spa/Search" element={<SearchApp />} />
        <Route path="/Spa/DatabaseConnections" element={<DatabaseConnectionsApp />} />
        <Route path="/Spa/DatabaseConnections/Register" element={<DatabaseConnectionRegisterApp />} />
        <Route path="/Spa/DatabaseConnections/:id/erd" element={<DatabaseConnectionErdRoute />} />
        <Route path="/Spa/DatabaseConnections/:id" element={<DatabaseConnectionDetailRoute />} />
        <Route path="/Spa/Documentation/Export" element={<DocumentationExportApp />} />
        <Route path="/Spa/Refactor/Analyze" element={<RefactorAnalyzeApp />} />
        <Route path="/Spa/Refactor/Plans" element={<RefactorPlansApp />} />
        <Route path="/Spa/Settings/*" element={<SettingsApp />} />
        <Route path="/Spa/Admin/*" element={<AdminApp />} />
        <Route path="/Spa/Insights" element={<InsightsApp />} />
        <Route path="/Spa/PortfolioHealth" element={<PortfolioHealthApp />} />
        <Route path="/Spa/Portfolio" element={<PortfolioHubApp />} />
        <Route path="*" element={<UnknownSpaRoute />} />
      </Routes>
    </BrowserRouter>
  );
}

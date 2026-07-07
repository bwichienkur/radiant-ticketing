import { NavLink, useLocation } from 'react-router-dom';
import { SpaLink } from '../SpaLink';

const WORKSPACE_SECTIONS = [
  { to: '/Spa/Settings/General', label: 'General' },
  { to: '/Spa/Settings/Authentication', label: 'Authentication' },
  { to: '/Spa/Settings/ApiKeys', label: 'API keys' },
  { to: '/Spa/Settings/Teams', label: 'Teams' },
  { to: '/Spa/Settings/Webhooks', label: 'Webhooks' },
  { to: '/Spa/Settings/Branding', label: 'Branding' },
  { to: '/Spa/Account/Notifications', label: 'Notifications' },
] as const;

const PLATFORM_SECTIONS = [
  { to: '/Spa/Admin/Jobs', label: 'Background jobs' },
  { to: '/Spa/Admin/Compliance', label: 'Compliance' },
  { to: '/Spa/Admin/CustomFields', label: 'Custom fields' },
  { to: '/Spa/Admin/Tenancy', label: 'Tenancy & billing' },
  { to: '/Spa/Admin/Observability', label: 'Observability' },
  { to: '/Spa/Admin/DataScaling', label: 'Data scaling' },
  { to: '/Spa/Admin/Retention', label: 'Retention' },
  { to: '/Spa/Admin/Delivery', label: 'Delivery' },
  { to: '/Spa/Admin/AiPrompts', label: 'AI prompts' },
] as const;

const RELATED_LINKS = [{ to: '/Spa/Insights', label: 'ROI insights' }] as const;

function isNavActive(pathname: string, to: string): boolean {
  return pathname === to || pathname.startsWith(`${to}/`);
}

function NavItem({ to, label }: { to: string; label: string }) {
  const location = useLocation();
  const active = isNavActive(location.pathname, to);

  if (to.startsWith('/Spa/Account/')) {
    return (
      <SpaLink href={to} className={`settings-nav-link ${active ? 'active' : ''}`.trim()}>
        {label}
      </SpaLink>
    );
  }

  return (
    <NavLink
      to={to}
      className={() => `settings-nav-link ${active ? 'active' : ''}`.trim()}
    >
      {label}
    </NavLink>
  );
}

export function SettingsNav() {
  return (
    <nav className="col-lg-3 mb-4 mb-lg-0" aria-label="Settings sections">
      <div className="card-panel p-3 eh-settings-nav">
        <div className="small text-uppercase text-muted mb-2 px-2">Workspace</div>
        <div className="d-flex flex-column gap-1">
          {WORKSPACE_SECTIONS.map((section) => (
            <NavItem key={section.to} to={section.to} label={section.label} />
          ))}
        </div>

        <div className="small text-uppercase text-muted mt-4 mb-2 px-2">Platform</div>
        <div className="d-flex flex-column gap-1">
          {PLATFORM_SECTIONS.map((section) => (
            <NavItem key={section.to} to={section.to} label={section.label} />
          ))}
        </div>

        <div className="small text-uppercase text-muted mt-4 mb-2 px-2">Related</div>
        <div className="d-flex flex-column gap-1">
          {RELATED_LINKS.map((link) => (
            <NavItem key={link.to} to={link.to} label={link.label} />
          ))}
        </div>
      </div>
    </nav>
  );
}

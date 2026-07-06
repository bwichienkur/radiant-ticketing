import { NavLink } from 'react-router-dom';

const PRIMARY_SECTIONS = [
  { to: '/Spa/Settings/General', label: 'General' },
  { to: '/Spa/Settings/Authentication', label: 'Authentication' },
  { to: '/Spa/Settings/ApiKeys', label: 'API keys' },
  { to: '/Spa/Settings/Teams', label: 'Teams' },
  { to: '/Spa/Settings/Webhooks', label: 'Webhooks' },
] as const;

const ADVANCED_ADMIN_LINKS = [
  { href: '/Admin/Jobs', label: 'Jobs' },
  { href: '/Spa/Insights', label: 'ROI' },
  { href: '/Admin/Compliance', label: 'Compliance' },
  { href: '/Admin/CustomFields', label: 'Custom fields' },
] as const;

export function SettingsNav() {
  return (
    <nav className="col-lg-3 mb-4 mb-lg-0" aria-label="Settings sections">
      <div className="card-panel p-3">
        <div className="small text-uppercase text-muted mb-2 px-2">Settings</div>
        <div className="d-flex flex-column gap-1">
          {PRIMARY_SECTIONS.map((section) => (
            <NavLink
              key={section.to}
              to={section.to}
              className={({ isActive }) =>
                `settings-nav-link ${isActive ? 'active' : ''}`.trim()
              }
            >
              {section.label}
            </NavLink>
          ))}
        </div>
        <div className="small text-uppercase text-muted mt-4 mb-2 px-2">Advanced admin</div>
        <div className="d-flex flex-column gap-1">
          {ADVANCED_ADMIN_LINKS.map((link) => (
            <a key={link.href} href={link.href} className="settings-nav-link">
              {link.label}
            </a>
          ))}
        </div>
        <p className="small text-muted mt-3 mb-0 px-2">
          More admin pages remain in Razor while Wave 2 migrations continue.
        </p>
      </div>
    </nav>
  );
}

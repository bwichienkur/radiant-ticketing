import { NavLink } from 'react-router-dom';
import { SpaLink } from '../SpaLink';

const ADMIN_SECTIONS = [
  { to: '/Spa/Admin/Jobs', label: 'Jobs' },
  { to: '/Spa/Admin/Compliance', label: 'Compliance' },
  { to: '/Spa/Admin/CustomFields', label: 'Custom fields' },
  { to: '/Spa/Admin/Tenancy', label: 'Tenancy' },
  { to: '/Spa/Admin/Observability', label: 'Observability' },
  { to: '/Spa/Admin/DataScaling', label: 'Data scaling' },
  { to: '/Spa/Admin/Retention', label: 'Retention' },
  { to: '/Spa/Admin/Delivery', label: 'Delivery' },
  { to: '/Spa/Admin/AiPrompts', label: 'AI prompts' },
] as const;

const RELATED_LINKS = [
  { href: '/Spa/Settings/General', label: 'Settings' },
  { href: '/Spa/Insights', label: 'ROI insights' },
] as const;

export function AdminNav() {
  return (
    <nav className="col-lg-3 mb-4 mb-lg-0" aria-label="Admin sections">
      <div className="card-panel p-3">
        <div className="small text-uppercase text-muted mb-2 px-2">Platform admin</div>
        <div className="d-flex flex-column gap-1">
          {ADMIN_SECTIONS.map((section) => (
            <NavLink
              key={section.to}
              to={section.to}
              className={({ isActive }) => `settings-nav-link ${isActive ? 'active' : ''}`.trim()}
            >
              {section.label}
            </NavLink>
          ))}
        </div>
        <div className="small text-uppercase text-muted mt-4 mb-2 px-2">Related</div>
        <div className="d-flex flex-column gap-1">
          {RELATED_LINKS.map((link) => (
            <SpaLink key={link.href} href={link.href} className="settings-nav-link">
              {link.label}
            </SpaLink>
          ))}
        </div>
      </div>
    </nav>
  );
}

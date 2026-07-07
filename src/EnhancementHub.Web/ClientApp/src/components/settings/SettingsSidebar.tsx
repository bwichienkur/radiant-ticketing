import { NavLink, useLocation } from 'react-router-dom';
import { SpaLink } from '../SpaLink';
import { SETTINGS_GROUPS, SETTINGS_SECTIONS } from '../../settings/settingsCatalog';
import { SETTINGS_CATEGORIES } from '../../settings/settingsCatalog';
import { SettingsCategoryIcon } from './SettingsIcons';

function isActive(pathname: string, route: string): boolean {
  return pathname === route || pathname.startsWith(`${route}/`);
}

export function SettingsSidebar() {
  const location = useLocation();

  return (
    <aside className="eh-settings-sidebar" aria-label="Settings navigation">
      <div className="eh-settings-sidebar__header">
        <SpaLink href="/Spa/Settings" className="eh-settings-sidebar__home">
          ← All settings
        </SpaLink>
      </div>

      {SETTINGS_GROUPS.map((group) => {
        const sections = SETTINGS_SECTIONS.filter((s) => s.groupId === group.id);
        if (sections.length === 0) {
          return null;
        }
        return (
          <div key={group.id} className="eh-settings-sidebar__group">
            <div className="eh-settings-sidebar__group-label">{group.title}</div>
            <nav className="eh-settings-sidebar__nav">
              {sections.map((section) => {
                const category = SETTINGS_CATEGORIES.find((c) => c.id === section.categoryId);
                const active = isActive(location.pathname, section.route);
                const isExternal = section.route.startsWith('/Spa/Account/') || section.route === '/Spa/Audit';

                const className = `eh-settings-sidebar__link ${active ? 'eh-settings-sidebar__link--active' : ''}`.trim();

                const inner = (
                  <>
                    <span className="eh-settings-sidebar__indicator" aria-hidden="true" />
                    <SettingsCategoryIcon
                      name={category?.icon ?? 'workspace'}
                      className="eh-settings-sidebar__icon"
                    />
                    <span className="eh-settings-sidebar__label">{section.title}</span>
                  </>
                );

                if (isExternal) {
                  return (
                    <SpaLink key={section.id} href={section.route} className={className}>
                      {inner}
                    </SpaLink>
                  );
                }

                return (
                  <NavLink key={section.id} to={section.route} className={className}>
                    {inner}
                  </NavLink>
                );
              })}
            </nav>
          </div>
        );
      })}
    </aside>
  );
}

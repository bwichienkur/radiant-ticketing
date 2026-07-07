import { NavLink, useNavigate } from 'react-router-dom';
import type { SettingsCategoryDef } from '../../settings/settingsCatalog';
import { getCategorySettingCount, getSectionsForCategory } from '../../settings/settingsCatalog';
import { IconChevronRight, SettingsCategoryIcon } from './SettingsIcons';

interface SettingsCategoryCardProps {
  category: SettingsCategoryDef;
  onNavigate?: () => void;
}

export function SettingsCategoryCard({ category, onNavigate }: SettingsCategoryCardProps) {
  const navigate = useNavigate();
  const count = getCategorySettingCount(category.id);
  const sections = getSectionsForCategory(category.id);
  const primaryRoute = sections[0]?.route ?? `/Spa/Settings/Category/${category.id}`;

  return (
    <button
      type="button"
      className="eh-settings-category-card"
      onClick={() => {
        if (sections.length === 1) {
          navigate(sections[0].route);
        } else if (sections.length > 1) {
          navigate(`/Spa/Settings/Category/${category.id}`);
        } else {
          navigate(primaryRoute);
        }
        onNavigate?.();
      }}
    >
      <div className="eh-settings-category-card__icon-wrap">
        <SettingsCategoryIcon name={category.icon} className="eh-settings-category-card__icon" />
      </div>
      <div className="eh-settings-category-card__body">
        <div className="eh-settings-category-card__title-row">
          <h3 className="eh-settings-category-card__title">{category.title}</h3>
          <IconChevronRight className="eh-settings-category-card__chevron" />
        </div>
        <p className="eh-settings-category-card__description">{category.description}</p>
        <span className="eh-settings-category-card__meta">{count} settings · {sections.length} areas</span>
      </div>
    </button>
  );
}

interface SettingsSectionCardProps {
  title: string;
  description: string;
  route: string;
  settingCount: number;
  icon?: string;
}

export function SettingsSectionCard({ title, description, route, settingCount, icon = 'workspace' }: SettingsSectionCardProps) {
  return (
    <NavLink to={route} className="eh-settings-section-card">
      <div className="eh-settings-section-card__icon-wrap">
        <SettingsCategoryIcon name={icon} className="eh-settings-section-card__icon" />
      </div>
      <div className="eh-settings-section-card__body">
        <h3 className="eh-settings-section-card__title">{title}</h3>
        <p className="eh-settings-section-card__description">{description}</p>
        <span className="eh-settings-section-card__meta">{settingCount} settings</span>
      </div>
      <IconChevronRight className="eh-settings-section-card__chevron" />
    </NavLink>
  );
}

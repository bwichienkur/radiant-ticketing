import { useParams } from 'react-router-dom';
import {
  SETTINGS_CATEGORIES,
  SETTINGS_GROUPS,
  getCategoryById,
  getSectionsForCategory,
  getSectionsForGroup,
  type SettingsCategoryId,
} from '../../settings/settingsCatalog';
import { SettingsCategoryCard, SettingsSectionCard } from './SettingsCategoryCard';
import { SettingsCategoryIcon } from './SettingsIcons';
import { SettingsPageHeader } from './SettingsPageHeader';
import { SettingsUtilityBar } from './SettingsUtilityBar';
import { SpaLink } from '../SpaLink';

interface SettingsHubProps {
  onOpenAi: () => void;
}

export function SettingsHub({ onOpenAi }: SettingsHubProps) {
  return (
    <div className="eh-settings-hub">
      <SettingsPageHeader />
      <SettingsUtilityBar onOpenAi={onOpenAi} />

      <section className="eh-settings-hub-section" aria-labelledby="settings-areas-heading">
        <div className="eh-settings-hub-section__header">
          <h2 id="settings-areas-heading" className="eh-settings-hub-section__title">
            Administration areas
          </h2>
          <p className="eh-settings-hub-section__description">
            Browse by domain — workspace, security, platform, AI, and integrations.
          </p>
        </div>
        <div className="eh-settings-category-grid">
          {SETTINGS_CATEGORIES.map((category) => (
            <SettingsCategoryCard key={category.id} category={category} />
          ))}
        </div>
      </section>

      {SETTINGS_GROUPS.map((group) => {
        const sections = getSectionsForGroup(group.id);
        if (sections.length === 0) {
          return null;
        }
        return (
          <section key={group.id} className="eh-settings-hub-section" aria-labelledby={`group-${group.id}`}>
            <div className="eh-settings-hub-section__header">
              <h2 id={`group-${group.id}`} className="eh-settings-hub-section__title">
                {group.title}
              </h2>
              <p className="eh-settings-hub-section__description">{group.description}</p>
            </div>
            <div className="eh-settings-section-grid">
              {sections.map((section) => {
                const category = SETTINGS_CATEGORIES.find((c) => c.id === section.categoryId);
                return (
                  <SettingsSectionCard
                    key={section.id}
                    title={section.title}
                    description={section.description}
                    route={section.route}
                    settingCount={section.settingCount}
                    icon={category?.icon}
                  />
                );
              })}
            </div>
          </section>
        );
      })}
    </div>
  );
}

export function SettingsCategoryLanding({ onOpenAi }: SettingsHubProps) {
  const { categoryId } = useParams<{ categoryId: SettingsCategoryId }>();
  const category = categoryId ? getCategoryById(categoryId) : undefined;
  const sections = categoryId ? getSectionsForCategory(categoryId) : [];

  if (!category) {
    return null;
  }

  return (
    <div className="eh-settings-hub">
      <nav className="eh-settings-breadcrumb" aria-label="Breadcrumb">
        <SpaLink href="/Spa/Settings">Settings</SpaLink>
        <span aria-hidden="true">/</span>
        <span aria-current="page">{category.title}</span>
      </nav>
      <header className="eh-settings-category-hero">
        <div className="eh-settings-category-hero__icon-wrap">
          <SettingsCategoryIcon name={category.icon} className="eh-settings-category-hero__icon" />
        </div>
        <div>
          <h1 className="eh-settings-category-hero__title">{category.title}</h1>
          <p className="eh-settings-category-hero__description">{category.description}</p>
        </div>
      </header>
      <SettingsUtilityBar onOpenAi={onOpenAi} />
      <section className="eh-settings-hub-section">
        <div className="eh-settings-section-grid">
          {sections.map((section) => (
            <SettingsSectionCard
              key={section.id}
              title={section.title}
              description={section.description}
              route={section.route}
              settingCount={section.settingCount}
              icon={category.icon}
            />
          ))}
        </div>
      </section>
    </div>
  );
}

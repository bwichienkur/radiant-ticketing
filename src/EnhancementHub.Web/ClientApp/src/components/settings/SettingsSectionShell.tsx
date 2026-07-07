import { type ReactNode, useEffect, useState } from 'react';
import type { SettingsSectionDef } from '../../settings/settingsCatalog';
import { getSectionById } from '../../settings/settingsCatalog';
import {
  isFavoriteSetting,
  pushRecentSetting,
  toggleFavoriteSetting,
} from '../../settings/settingsStorage';
import { SpaLink } from '../SpaLink';
import { IconStar, SettingsCategoryIcon } from './SettingsIcons';
import { SETTINGS_CATEGORIES } from '../../settings/settingsCatalog';

interface SettingsSectionShellProps {
  section: SettingsSectionDef;
  children: ReactNode;
}

export function SettingsSectionShell({ section, children }: SettingsSectionShellProps) {
  const [favorite, setFavorite] = useState(false);
  const category = SETTINGS_CATEGORIES.find((c) => c.id === section.categoryId);
  const related = (section.relatedIds ?? [])
    .map((id) => getSectionById(id))
    .filter((s): s is SettingsSectionDef => Boolean(s));

  useEffect(() => {
    pushRecentSetting(section.id);
    setFavorite(isFavoriteSetting(section.id));
  }, [section.id]);

  return (
    <div className="eh-settings-section-shell">
      <nav className="eh-settings-breadcrumb" aria-label="Breadcrumb">
        <SpaLink href="/Spa/Settings">Settings</SpaLink>
        {category ? (
          <>
            <span aria-hidden="true">/</span>
            <SpaLink href={`/Spa/Settings/Category/${category.id}`}>{category.title}</SpaLink>
          </>
        ) : null}
        <span aria-hidden="true">/</span>
        <span aria-current="page">{section.title}</span>
      </nav>

      <header className="eh-settings-section-hero">
        <div className="eh-settings-section-hero__icon-wrap">
          <SettingsCategoryIcon name={category?.icon ?? 'workspace'} className="eh-settings-section-hero__icon" />
        </div>
        <div className="eh-settings-section-hero__copy">
          <div className="eh-settings-section-hero__title-row">
            <h1 className="eh-settings-section-hero__title">{section.title}</h1>
            <button
              type="button"
              className={`eh-settings-favorite-btn ${favorite ? 'eh-settings-favorite-btn--active' : ''}`.trim()}
              aria-label={favorite ? 'Remove from favorites' : 'Add to favorites'}
              aria-pressed={favorite}
              onClick={() => setFavorite(toggleFavoriteSetting(section.id).includes(section.id))}
            >
              <IconStar />
            </button>
          </div>
          <p className="eh-settings-section-hero__description">{section.description}</p>
          {section.docHint ? (
            <p className="eh-settings-section-hero__help">{section.docHint}</p>
          ) : null}
        </div>
      </header>

      <div className="eh-settings-section-content">{children}</div>

      {related.length > 0 ? (
        <footer className="eh-settings-related">
          <h2 className="eh-settings-related__title">Related settings</h2>
          <div className="eh-settings-related__links">
            {related.map((rel) => (
              <SpaLink key={rel.id} href={rel.route} className="eh-settings-related__link">
                {rel.title}
              </SpaLink>
            ))}
          </div>
        </footer>
      ) : null}
    </div>
  );
}

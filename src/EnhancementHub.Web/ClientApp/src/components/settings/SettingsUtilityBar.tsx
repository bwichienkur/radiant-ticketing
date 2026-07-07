import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { searchSettings, SETTINGS_SECTIONS } from '../../settings/settingsCatalog';
import { readFavoriteSettings, readRecentSettings } from '../../settings/settingsStorage';
import { getSectionById } from '../../settings/settingsCatalog';
import { IconSearch, IconSparkle } from './SettingsIcons';

interface SettingsUtilityBarProps {
  onOpenAi: () => void;
  onSearchFocus?: () => void;
}

function highlightMatch(text: string, query: string) {
  if (!query) {
    return text;
  }
  const idx = text.toLowerCase().indexOf(query.toLowerCase());
  if (idx < 0) {
    return text;
  }
  return (
    <>
      {text.slice(0, idx)}
      <mark className="eh-settings-search-mark">{text.slice(idx, idx + query.length)}</mark>
      {text.slice(idx + query.length)}
    </>
  );
}

export function SettingsUtilityBar({ onOpenAi, onSearchFocus }: SettingsUtilityBarProps) {
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(false);
  const [recent, setRecent] = useState<string[]>([]);
  const [favorites, setFavorites] = useState<string[]>([]);

  useEffect(() => {
    setRecent(readRecentSettings());
    setFavorites(readFavoriteSettings());
  }, []);

  const results = useMemo(() => searchSettings(query), [query]);

  function goTo(route: string) {
    setOpen(false);
    setQuery('');
    navigate(route);
  }

  return (
    <div className="eh-settings-utility-bar">
      <div className="eh-settings-utility-bar__search-wrap">
        <IconSearch className="eh-settings-utility-bar__search-icon" />
        <input
          type="search"
          className="eh-settings-utility-search"
          placeholder="Search settings…"
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setOpen(true);
          }}
          onFocus={() => {
            setOpen(true);
            onSearchFocus?.();
          }}
          onBlur={() => window.setTimeout(() => setOpen(false), 180)}
          aria-label="Search settings"
          aria-expanded={open}
        />
        <kbd className="eh-kbd eh-settings-utility-kbd">/</kbd>
        {open && (query || recent.length > 0) ? (
          <div className="eh-settings-search-panel" role="listbox">
            {query ? (
              results.length > 0 ? (
                results.map((section) => (
                  <button
                    key={section.id}
                    type="button"
                    className="eh-settings-search-result"
                    role="option"
                    onMouseDown={() => goTo(section.route)}
                  >
                    <span className="eh-settings-search-result__title">
                      {highlightMatch(section.title, query)}
                    </span>
                    <span className="eh-settings-search-result__meta">
                      {highlightMatch(section.description, query)}
                    </span>
                  </button>
                ))
              ) : (
                <div className="eh-settings-search-empty">No settings match &ldquo;{query}&rdquo;</div>
              )
            ) : (
              <>
                {favorites.length > 0 ? (
                  <div className="eh-settings-search-group">
                    <div className="eh-settings-search-group__label">Favorites</div>
                    {favorites.map((id) => {
                      const section = getSectionById(id);
                      if (!section) return null;
                      return (
                        <button
                          key={id}
                          type="button"
                          className="eh-settings-search-result"
                          onMouseDown={() => goTo(section.route)}
                        >
                          <span className="eh-settings-search-result__title">{section.title}</span>
                        </button>
                      );
                    })}
                  </div>
                ) : null}
                {recent.length > 0 ? (
                  <div className="eh-settings-search-group">
                    <div className="eh-settings-search-group__label">Recently viewed</div>
                    {recent.map((id) => {
                      const section = getSectionById(id);
                      if (!section) return null;
                      return (
                        <button
                          key={id}
                          type="button"
                          className="eh-settings-search-result"
                          onMouseDown={() => goTo(section.route)}
                        >
                          <span className="eh-settings-search-result__title">{section.title}</span>
                        </button>
                      );
                    })}
                  </div>
                ) : null}
              </>
            )}
          </div>
        ) : null}
      </div>

      <div className="eh-settings-utility-bar__meta">
        <span className="eh-settings-utility-hint d-none d-md-inline">
          {SETTINGS_SECTIONS.length} settings areas
        </span>
        <button type="button" className="eh-settings-utility-ai-btn" onClick={onOpenAi}>
          <IconSparkle />
          <span>AI Assistant</span>
        </button>
      </div>
    </div>
  );
}

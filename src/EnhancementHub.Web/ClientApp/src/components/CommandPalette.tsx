import { useCallback, useEffect, useRef, useState } from 'react';
import { searchGlobalGrouped } from '../api/spaClient';
import type { GlobalSearchItem } from '../types/spa';

interface PaletteItem {
  type: string;
  title: string;
  subtitle?: string;
  url: string;
}

const RECENT_SEARCHES_KEY = 'eh-recent-searches';
const NAV_PAGES: PaletteItem[] = [
  { type: 'page', title: 'Dashboard', subtitle: 'Navigate', url: '/' },
  { type: 'page', title: 'New request', subtitle: 'Create', url: '/Spa/CreateRequest' },
  { type: 'page', title: 'Requests', subtitle: 'Navigate', url: '/Spa/RequestList' },
  { type: 'page', title: 'Approvals', subtitle: 'Navigate', url: '/Spa/ApprovalQueue' },
  { type: 'page', title: 'Portfolio overview', subtitle: 'Navigate', url: '/Spa/Portfolio' },
  { type: 'page', title: 'Applications', subtitle: 'Portfolio', url: '/Spa/Applications' },
  { type: 'page', title: 'System map', subtitle: 'Portfolio', url: '/Spa/SystemMap' },
  { type: 'page', title: 'Repositories', subtitle: 'Portfolio', url: '/Spa/Repositories' },
  { type: 'page', title: 'Databases', subtitle: 'Portfolio', url: '/Spa/DatabaseConnections' },
  { type: 'page', title: 'Schema drift', subtitle: 'Portfolio', url: '/Spa/SchemaDrift' },
  { type: 'page', title: 'Global search', subtitle: 'Governance', url: '/Spa/Search' },
  { type: 'page', title: 'Audit log', subtitle: 'Governance', url: '/Spa/Audit' },
  { type: 'page', title: 'Insights', subtitle: 'Governance', url: '/Spa/Insights' },
  { type: 'page', title: 'Portfolio health', subtitle: 'Governance', url: '/Spa/PortfolioHealth' },
  { type: 'page', title: 'Settings', subtitle: 'Admin', url: '/Spa/Settings' },
  { type: 'page', title: 'Onboarding wizard', subtitle: 'Setup', url: '/Spa/OnboardingWizard' },
];

function loadRecentSearches(): string[] {
  try {
    const raw = localStorage.getItem(RECENT_SEARCHES_KEY);
    if (!raw) {
      return [];
    }
    const parsed = JSON.parse(raw) as unknown;
    return Array.isArray(parsed) ? parsed.filter((v): v is string => typeof v === 'string') : [];
  } catch {
    return [];
  }
}

function saveRecentSearch(query: string) {
  const trimmed = query.trim();
  if (trimmed.length < 2) {
    return;
  }

  const recent = loadRecentSearches().filter((item) => item !== trimmed);
  recent.unshift(trimmed);
  localStorage.setItem(RECENT_SEARCHES_KEY, JSON.stringify(recent.slice(0, 8)));
}

function toPaletteItem(item: GlobalSearchItem): PaletteItem {
  return {
    type: item.type,
    title: item.title,
    subtitle: item.subtitle,
    url: item.url,
  };
}

function filterPages(term: string): PaletteItem[] {
  const normalized = term.toLowerCase();
  return NAV_PAGES.filter((page) => page.title.toLowerCase().includes(normalized));
}

export function CommandPalette() {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [items, setItems] = useState<PaletteItem[]>(NAV_PAGES);
  const [semanticHint, setSemanticHint] = useState<string | null>(null);
  const [activeIndex, setActiveIndex] = useState(0);
  const [loading, setLoading] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const debounceRef = useRef<number>();

  const close = useCallback(() => {
    setOpen(false);
    setQuery('');
    setSemanticHint(null);
    setActiveIndex(0);
  }, []);

  const openPalette = useCallback(() => {
    setOpen(true);
    setQuery('');
    setSemanticHint(null);
    setItems(NAV_PAGES);
    setActiveIndex(0);
    window.setTimeout(() => inputRef.current?.focus(), 50);
  }, []);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        if (open) {
          close();
        } else {
          openPalette();
        }
      }

      if (event.key === 'Escape' && open) {
        event.preventDefault();
        close();
      }
    }

    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [close, open, openPalette]);

  useEffect(() => {
    const trigger = document.querySelector('[data-command-trigger]');
    if (!trigger) {
      return;
    }

    function onClick() {
      openPalette();
    }

    trigger.addEventListener('click', onClick);
    return () => trigger.removeEventListener('click', onClick);
  }, [openPalette]);

  useEffect(() => {
    window.clearTimeout(debounceRef.current);
    if (!open) {
      return;
    }

    const trimmed = query.trim();
    if (trimmed.length < 2) {
      setItems(filterPages(trimmed));
      setSemanticHint(null);
      setActiveIndex(0);
      return;
    }

    debounceRef.current = window.setTimeout(() => {
      void (async () => {
        setLoading(true);
        try {
          const result = await searchGlobalGrouped(trimmed, 12, true);
          saveRecentSearch(trimmed);
          const mapped = result.items.map(toPaletteItem);
          setItems(mapped.length > 0 ? mapped : filterPages(trimmed));
          setSemanticHint(result.semanticHint ?? null);
          setActiveIndex(0);
        } catch {
          setItems(filterPages(trimmed));
          setSemanticHint(null);
        } finally {
          setLoading(false);
        }
      })();
    }, 200);

    return () => window.clearTimeout(debounceRef.current);
  }, [open, query]);

  function navigateTo(url: string) {
    close();
    if (url.startsWith('/Spa/') || url === '/' || url === '/Index') {
      window.dispatchEvent(new CustomEvent('eh-spa-navigate', { detail: { path: url } }));
      return;
    }

    window.location.href = url;
  }

  function onInputKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    if (items.length === 0) {
      return;
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      setActiveIndex((index) => (index + 1) % items.length);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      setActiveIndex((index) => (index - 1 + items.length) % items.length);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const target = items[activeIndex];
      if (target) {
        navigateTo(target.url);
      }
    }
  }

  if (!open) {
    return null;
  }

  return (
    <div className="command-palette-backdrop eh-command-palette" role="presentation" onClick={close}>
      <div
        className="command-palette-modal eh-command-palette-modal"
        role="dialog"
        aria-modal="true"
        aria-label="Command palette"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="command-palette-input-wrap eh-command-palette-input-wrap">
          <svg className="eh-command-search-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <circle cx="11" cy="11" r="7" />
            <path d="m20 20-3.5-3.5" />
          </svg>
          <input
            ref={inputRef}
            id="commandPaletteInput"
            type="search"
            className="form-control eh-command-palette-input"
            placeholder="Search workspace…"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            onKeyDown={onInputKeyDown}
            aria-controls="commandPaletteResults"
            autoComplete="off"
          />
          <kbd className="eh-kbd eh-command-esc">ESC</kbd>
        </div>
        {loading ? <p className="eh-command-status">Searching…</p> : null}
        {semanticHint ? <p className="eh-command-hint">{semanticHint}</p> : null}
        <div id="commandPaletteResults" className="command-palette-results eh-command-palette-results" role="listbox">
          {items.length === 0 ? (
            <p className="eh-command-empty">No results</p>
          ) : (
            items.map((item, index) => (
              <button
                key={`${item.type}-${item.url}-${item.title}`}
                type="button"
                className={`command-result eh-command-result ${index === activeIndex ? 'active' : ''}`}
                role="option"
                aria-selected={index === activeIndex}
                onMouseEnter={() => setActiveIndex(index)}
                onClick={() => navigateTo(item.url)}
              >
                <span className="eh-command-result-icon" aria-hidden="true">{item.type.slice(0, 1).toUpperCase()}</span>
                <span className="eh-command-result-body">
                  <span className="command-result-title">{item.title}</span>
                  {item.subtitle ? <span className="command-result-sub">{item.subtitle}</span> : null}
                </span>
                <span className="command-result-type">{item.type}</span>
              </button>
            ))
          )}
          {query.trim().length >= 2 ? (
            <div className="eh-command-footer">
              <button
                type="button"
                className="eh-command-footer-link"
                onClick={() => navigateTo(`/Spa/Search?q=${encodeURIComponent(query.trim())}`)}
              >
                View all grouped results
              </button>
            </div>
          ) : null}
        </div>
        <div className="eh-command-palette-shortcuts" aria-hidden="true">
          <span><kbd className="eh-kbd">↑↓</kbd> navigate</span>
          <span><kbd className="eh-kbd">↵</kbd> open</span>
          <span><kbd className="eh-kbd">esc</kbd> close</span>
        </div>
      </div>
    </div>
  );
}

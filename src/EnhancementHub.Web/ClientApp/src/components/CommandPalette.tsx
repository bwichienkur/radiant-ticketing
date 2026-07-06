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
  { type: 'page', title: 'Requests', subtitle: 'Navigate', url: '/Spa/RequestList' },
  { type: 'page', title: 'Approvals', subtitle: 'Navigate', url: '/Spa/ApprovalQueue' },
  { type: 'page', title: 'System map', subtitle: 'Navigate', url: '/Spa/SystemMap' },
  { type: 'page', title: 'Insights', subtitle: 'Navigate', url: '/Spa/Insights' },
  { type: 'page', title: 'Settings', subtitle: 'Navigate', url: '/Spa/Settings' },
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
    <div className="command-palette-backdrop" role="presentation" onClick={close}>
      <div
        className="command-palette-modal card-panel"
        role="dialog"
        aria-modal="true"
        aria-label="Command palette"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="command-palette-input-wrap border-bottom px-3 py-2">
          <input
            ref={inputRef}
            id="commandPaletteInput"
            type="search"
            className="form-control border-0 shadow-none"
            placeholder="Search requests, apps, symbols, pages…"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            onKeyDown={onInputKeyDown}
            aria-controls="commandPaletteResults"
            autoComplete="off"
          />
          {loading ? <span className="small text-muted">Searching…</span> : null}
          {semanticHint ? <p className="small text-muted mb-0 mt-1">{semanticHint}</p> : null}
        </div>
        <div id="commandPaletteResults" className="command-palette-results" role="listbox">
          {items.length === 0 ? (
            <p className="text-muted small px-3 py-2 mb-0">No results</p>
          ) : (
            items.map((item, index) => (
              <button
                key={`${item.type}-${item.url}-${item.title}`}
                type="button"
                className={`command-result w-100 text-start ${index === activeIndex ? 'active' : ''}`}
                role="option"
                aria-selected={index === activeIndex}
                onMouseEnter={() => setActiveIndex(index)}
                onClick={() => navigateTo(item.url)}
              >
                <span className="command-result-type">{item.type}</span>
                <span className="command-result-title">{item.title}</span>
                <span className="command-result-sub">{item.subtitle ?? ''}</span>
              </button>
            ))
          )}
          {query.trim().length >= 2 ? (
            <div className="border-top px-3 py-2">
              <button
                type="button"
                className="btn btn-link btn-sm p-0"
                onClick={() => navigateTo(`/Spa/Search?q=${encodeURIComponent(query.trim())}`)}
              >
                View all grouped results
              </button>
            </div>
          ) : null}
        </div>
      </div>
    </div>
  );
}

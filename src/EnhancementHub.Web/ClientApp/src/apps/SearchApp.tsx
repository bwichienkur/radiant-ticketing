import { FormEvent, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { searchGlobalGrouped } from '../api/spaClient';
import { SpaLink } from '../components/SpaLink';
import { EmptyState, ErrorState, LoadingState, PageHeader } from '../components/ui';
import type { GlobalSearchResult } from '../types/spa';

const GROUP_LABELS: Record<string, string> = {
  page: 'Pages',
  request: 'Requests',
  application: 'Applications',
  repository: 'Repositories',
  drift: 'Schema drift',
  symbol: 'Code symbols',
  artifact: 'Indexed artifacts',
};

export function SearchApp() {
  const [searchParams, setSearchParams] = useSearchParams();
  const initialQuery = searchParams.get('q') ?? '';
  const [query, setQuery] = useState(initialQuery);
  const [result, setResult] = useState<GlobalSearchResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setQuery(initialQuery);
    if (initialQuery.trim().length >= 2) {
      void runSearch(initialQuery);
    } else {
      setResult(null);
    }
  }, [initialQuery]);

  async function runSearch(term: string) {
    const trimmed = term.trim();
    if (trimmed.length < 2) {
      setResult(null);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      setResult(await searchGlobalGrouped(trimmed));
    } catch {
      setError('Search failed. Try again.');
    } finally {
      setLoading(false);
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmed = query.trim();
    setSearchParams(trimmed ? { q: trimmed } : {});
  }

  return (
    <div aria-live="polite">
      <PageHeader
        title="Search"
        description="Find requests, applications, repositories, drift findings, and indexed code symbols."
      />

      <form className="card-panel p-3 mb-3" onSubmit={handleSubmit}>
        <label className="form-label" htmlFor="global-search-input">
          Search across the platform
        </label>
        <div className="input-group">
          <input
            id="global-search-input"
            className="form-control"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder='Try "order cancellation"'
            autoComplete="off"
          />
          <button type="submit" className="btn btn-primary">
            Search
          </button>
        </div>
        <p className="form-text mb-0">Tip: press Ctrl+K (or ⌘K) from anywhere to open quick search.</p>
      </form>

      {loading ? <LoadingState label="Searching…" /> : null}
      {error ? <ErrorState message={error} onRetry={() => void runSearch(query)} /> : null}

      {!loading && !error && result && result.items.length === 0 ? (
        <EmptyState
          title="No results"
          description={`Nothing matched "${result.query}". Try a shorter keyword or check spelling.`}
          icon="search"
        />
      ) : null}

      {!loading && !error && result && result.items.length > 0 ? (
        <div className="search-results-groups">
          {Object.entries(result.groups).map(([group, items]) => (
            <section key={group} className="card-panel mb-3" aria-label={GROUP_LABELS[group] ?? group}>
              <div className="card-panel-header px-3 py-2 border-bottom">
                <h2 className="h6 mb-0">{GROUP_LABELS[group] ?? group}</h2>
              </div>
              <ul className="list-group list-group-flush">
                {items.map((item) => (
                  <li key={`${item.type}:${item.url}:${item.title}`} className="list-group-item">
                    <SpaLink href={item.url} className="text-decoration-none d-block">
                      <strong>{item.title}</strong>
                      {item.subtitle ? <div className="small text-muted">{item.subtitle}</div> : null}
                    </SpaLink>
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </div>
      ) : null}
    </div>
  );
}

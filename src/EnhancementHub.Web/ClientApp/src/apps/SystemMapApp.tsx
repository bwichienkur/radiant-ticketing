import { useEffect, useMemo, useState } from 'react';
import { getSystemMap, listApplications } from '../api/spaClient';
import { LoadingSkeleton } from '../components/LoadingSkeleton';
import { SystemMapGraph } from '../components/SystemMapGraph';
import { nodeColor } from '../components/systemMapGraph';
import type { ApplicationSummary, SystemMap } from '../types/spa';

interface SystemMapAppProps {
  initialApplicationId?: string;
}

type ViewMode = 'graph' | 'list';

export function SystemMapApp({ initialApplicationId }: SystemMapAppProps) {
  const [applications, setApplications] = useState<ApplicationSummary[]>([]);
  const [selectedId, setSelectedId] = useState(initialApplicationId ?? '');
  const [map, setMap] = useState<SystemMap | null>(null);
  const [viewMode, setViewMode] = useState<ViewMode>('graph');
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loadingApps, setLoadingApps] = useState(true);
  const [loadingMap, setLoadingMap] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function loadApps() {
      setLoadingApps(true);
      try {
        const apps = await listApplications();
        if (!cancelled) {
          setApplications(apps);
          if (!selectedId && apps.length > 0) {
            setSelectedId(apps[0].id);
          }
        }
      } catch {
        if (!cancelled) {
          setError('Failed to load applications.');
        }
      } finally {
        if (!cancelled) {
          setLoadingApps(false);
        }
      }
    }

    void loadApps();
    return () => {
      cancelled = true;
    };
  }, [selectedId]);

  useEffect(() => {
    if (!selectedId) {
      return;
    }

    let cancelled = false;

    async function loadMap() {
      setLoadingMap(true);
      setError(null);
      setSelectedNodeId(null);
      try {
        const result = await getSystemMap(selectedId);
        if (!cancelled) {
          setMap(result);
        }
      } catch {
        if (!cancelled) {
          setError('Failed to load system map.');
          setMap(null);
        }
      } finally {
        if (!cancelled) {
          setLoadingMap(false);
        }
      }
    }

    void loadMap();
    return () => {
      cancelled = true;
    };
  }, [selectedId]);

  const nodesByType = useMemo(() => {
    if (!map) {
      return [];
    }

    const groups = new Map<string, typeof map.nodes>();
    for (const node of map.nodes) {
      const list = groups.get(node.type) ?? [];
      list.push(node);
      groups.set(node.type, list);
    }

    return Array.from(groups.entries()).sort(([a], [b]) => a.localeCompare(b));
  }, [map]);

  const selectedNode = useMemo(
    () => map?.nodes.find((node) => node.id === selectedNodeId) ?? null,
    [map, selectedNodeId],
  );

  if (loadingApps) {
    return (
      <div aria-busy="true">
        <p className="text-muted" role="status">
          Loading applications…
        </p>
        <LoadingSkeleton />
      </div>
    );
  }

  if (applications.length === 0) {
    return (
      <div className="card-panel p-4 text-center">
        <h2 className="h5">No applications yet</h2>
        <p className="text-muted mb-3">Register an application before viewing the system map.</p>
        <a href="/Spa/OnboardingWizard" className="btn btn-primary">
          Start onboarding wizard
        </a>
      </div>
    );
  }

  return (
    <div aria-live="polite">
      <div className="row g-3 mb-4">
        <div className="col-md-6">
          <label className="form-label" htmlFor="spa-app-select">
            Application
          </label>
          <select
            id="spa-app-select"
            className="form-select"
            value={selectedId}
            onChange={(event) => setSelectedId(event.target.value)}
          >
            {applications.map((app) => (
              <option key={app.id} value={app.id}>
                {app.name}
              </option>
            ))}
          </select>
        </div>
        <div className="col-md-6 d-flex align-items-end justify-content-md-end gap-2 flex-wrap">
          <div className="btn-group" role="group" aria-label="View mode">
            <button
              type="button"
              className={`btn btn-sm ${viewMode === 'graph' ? 'btn-primary' : 'btn-outline-primary'}`}
              onClick={() => setViewMode('graph')}
            >
              Graph
            </button>
            <button
              type="button"
              className={`btn btn-sm ${viewMode === 'list' ? 'btn-primary' : 'btn-outline-primary'}`}
              onClick={() => setViewMode('list')}
            >
              List
            </button>
          </div>
          <a href="/SystemMap/Index" className="btn btn-outline-secondary btn-sm">
            Classic map view
          </a>
        </div>
      </div>

      {error ? (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      ) : null}

      {loadingMap ? (
        <LoadingSkeleton />
      ) : map ? (
        <>
          <div className="row g-3 mb-4">
            <div className="col-md-4">
              <div className="stat-card">
                <div className="label">Nodes</div>
                <div className="value fs-5">{map.nodes.length}</div>
              </div>
            </div>
            <div className="col-md-4">
              <div className="stat-card">
                <div className="label">Edges</div>
                <div className="value fs-5">{map.edges.length}</div>
              </div>
            </div>
            <div className="col-md-4">
              <div className="stat-card">
                <div className="label">Built</div>
                <div className="value fs-6">
                  {map.builtAt ? new Date(map.builtAt).toLocaleString() : 'Not built yet'}
                </div>
              </div>
            </div>
          </div>

          {selectedNode ? (
            <div className="alert alert-light border mb-3">
              <strong>{selectedNode.label}</strong>
              <span className="badge ms-2" style={{ backgroundColor: nodeColor(selectedNode.type) }}>
                {selectedNode.type}
              </span>
              {selectedNode.detail ? <div className="small text-muted mt-1">{selectedNode.detail}</div> : null}
            </div>
          ) : null}

          {nodesByType.length === 0 ? (
            <div className="card-panel p-4">
              <p className="mb-0 text-muted">No graph nodes yet. Rebuild the graph from the classic system map page.</p>
            </div>
          ) : viewMode === 'graph' ? (
            <SystemMapGraph map={map} onNodeSelected={setSelectedNodeId} />
          ) : (
            <>
              {nodesByType.map(([type, nodes]) => (
                <section key={type} className="card-panel p-4 mb-3">
                  <h2 className="h6 text-uppercase text-muted mb-3">{type}</h2>
                  <div className="row g-2">
                    {nodes.map((node) => (
                      <div key={node.id} className="col-md-6 col-lg-4">
                        <div className="border rounded p-2 h-100">
                          <div className="fw-semibold">{node.label}</div>
                          {node.detail ? <div className="small text-muted">{node.detail}</div> : null}
                        </div>
                      </div>
                    ))}
                  </div>
                </section>
              ))}

              {map.edges.length > 0 ? (
                <section className="card-panel p-4">
                  <h2 className="h6 text-uppercase text-muted mb-3">Relationships</h2>
                  <ul className="list-unstyled mb-0">
                    {map.edges.slice(0, 50).map((edge) => (
                      <li key={`${edge.fromId}-${edge.toId}-${edge.label}`} className="small mb-1">
                        <code>{edge.fromId}</code> → <code>{edge.toId}</code>
                        {edge.label ? <span className="text-muted"> ({edge.label})</span> : null}
                      </li>
                    ))}
                  </ul>
                  {map.edges.length > 50 ? (
                    <p className="small text-muted mt-2 mb-0">Showing first 50 of {map.edges.length} edges.</p>
                  ) : null}
                </section>
              ) : null}
            </>
          )}
        </>
      ) : null}
    </div>
  );
}

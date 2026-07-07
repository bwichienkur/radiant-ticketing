import { useEffect, useMemo, useState } from 'react';
import { getSystemMap, listApplications, rebuildSystemMap } from '../api/spaClient';
import {
  AlertBanner,
  EmptyState,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  SectionCard,
} from '../components/ui';
import { SystemMapGraph } from '../components/SystemMapGraph';
import { SpaLink } from '../components/SpaLink';
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
  const [rebuilding, setRebuilding] = useState(false);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

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

  async function handleRebuild() {
    if (!selectedId || rebuilding) {
      return;
    }

    setRebuilding(true);
    setError(null);
    setStatusMessage(null);
    try {
      const result = await rebuildSystemMap(selectedId);
      setMap(result);
      setSelectedNodeId(null);
      setStatusMessage('System graph rebuilt successfully.');
    } catch {
      setError('Failed to rebuild system graph.');
    } finally {
      setRebuilding(false);
    }
  }

  if (loadingApps) {
    return <LoadingState label="Loading applications…" />;
  }

  if (applications.length === 0) {
    return (
      <EmptyState
        title="No applications yet"
        description="Register an application before viewing the system map."
        icon="document"
        action={
          <SpaLink href="/Spa/OnboardingWizard" className="btn btn-primary">
            Start onboarding wizard
          </SpaLink>
        }
      />
    );
  }

  return (
    <div aria-live="polite">
      <PageHeader
        title="System map"
        description="Explore dependencies and relationships across your application landscape."
      />

      <div className="card-panel p-3 mb-4">
        <div className="row g-3 align-items-end">
          <div className="col-md-6">
            <FormField id="spa-app-select" label="Application">
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
            </FormField>
          </div>
        <div className="col-md-6 d-flex align-items-end justify-content-md-end gap-2 flex-wrap">
          <button
            type="button"
            className="btn btn-sm btn-outline-primary"
            disabled={!selectedId || rebuilding || loadingMap}
            onClick={() => void handleRebuild()}
          >
            {rebuilding ? 'Rebuilding…' : 'Rebuild graph'}
          </button>
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
        </div>
        </div>
      </div>

      {statusMessage ? (
        <AlertBanner variant="success" className="mb-3">
          {statusMessage}
        </AlertBanner>
      ) : null}

      {error ? <ErrorState message={error} className="mb-3" /> : null}

      {loadingMap ? (
        <LoadingState label="Loading system map…" />
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
            <AlertBanner variant="neutral" className="mb-3">
              <strong>{selectedNode.label}</strong>
              <span className="badge ms-2" style={{ backgroundColor: nodeColor(selectedNode.type) }}>
                {selectedNode.type}
              </span>
              {selectedNode.detail ? <div className="small text-muted mt-1">{selectedNode.detail}</div> : null}
            </AlertBanner>
          ) : null}

          {nodesByType.length === 0 ? (
            <div className="card-panel p-4 d-flex flex-wrap justify-content-between align-items-center gap-3">
              <p className="mb-0 text-muted">No graph nodes yet. Run discovery, then rebuild the graph.</p>
              <button
                type="button"
                className="btn btn-primary btn-sm"
                disabled={rebuilding}
                onClick={() => void handleRebuild()}
              >
                {rebuilding ? 'Rebuilding…' : 'Rebuild graph'}
              </button>
            </div>
          ) : viewMode === 'graph' ? (
            <SystemMapGraph map={map} onNodeSelected={setSelectedNodeId} />
          ) : (
            <>
              {nodesByType.map(([type, nodes]) => (
                <SectionCard key={type} title={type}>
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
                </SectionCard>
              ))}

              {map.edges.length > 0 ? (
                <SectionCard title="Relationships">
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
                </SectionCard>
              ) : null}
            </>
          )}
        </>
      ) : null}
    </div>
  );
}

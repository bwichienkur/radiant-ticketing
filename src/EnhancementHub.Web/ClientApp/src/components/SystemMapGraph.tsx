import cytoscape, { type Core } from 'cytoscape';
import { useEffect, useRef } from 'react';
import type { SystemMap } from '../types/spa';
import { buildCytoscapeElements, buildCytoscapeStyles } from './systemMapGraph';

interface SystemMapGraphProps {
  map: SystemMap;
  onNodeSelected?: (nodeId: string | null) => void;
}

export function SystemMapGraph({ map, onNodeSelected }: SystemMapGraphProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const cyRef = useRef<Core | null>(null);

  useEffect(() => {
    if (!containerRef.current) {
      return;
    }

    const { elements } = buildCytoscapeElements(map.nodes, map.edges);

    const cy = cytoscape({
      container: containerRef.current,
      elements,
      style: buildCytoscapeStyles(),
      minZoom: 0.2,
      maxZoom: 2.5,
      wheelSensitivity: 0.2,
      layout: {
        name: 'cose',
        animate: false,
        padding: 40,
        nodeRepulsion: () => 8000,
        idealEdgeLength: () => 100,
      },
    });

    cy.on('tap', 'node', (event) => {
      onNodeSelected?.(event.target.id());
    });

    cy.on('tap', (event) => {
      if (event.target === cy) {
        onNodeSelected?.(null);
      }
    });

    cyRef.current = cy;

    return () => {
      cy.destroy();
      cyRef.current = null;
    };
  }, [map, onNodeSelected]);

  useEffect(() => {
    function onResize() {
      cyRef.current?.resize();
      cyRef.current?.fit(undefined, 40);
    }

    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  const { truncated, nodeCount, edgeCount } = buildCytoscapeElements(map.nodes, map.edges);

  return (
    <section className="card-panel p-3">
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-2">
        <h2 className="h6 mb-0">Interactive graph</h2>
        <span className="small text-muted">
          {nodeCount} nodes · {edgeCount} edges · scroll to zoom, drag to pan
        </span>
      </div>
      {truncated ? (
        <p className="small text-warning mb-2">
          Showing first {nodeCount} of {map.nodes.length} nodes for performance. Use list view for full data.
        </p>
      ) : null}
      <div
        ref={containerRef}
        className="system-map-graph-canvas"
        role="img"
        aria-label={`System map graph for ${map.applicationName ?? 'application'}`}
      />
    </section>
  );
}

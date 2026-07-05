import cytoscape, { type Core } from 'cytoscape';
import { useEffect, useRef, useState } from 'react';
import type { SystemMap } from '../types/spa';
import { buildCytoscapeElements, buildCytoscapeStyles, readGraphThemeFromDocument } from './systemMapGraph';

interface SystemMapGraphProps {
  map: SystemMap;
  onNodeSelected?: (nodeId: string | null) => void;
}

export function SystemMapGraph({ map, onNodeSelected }: SystemMapGraphProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const cyRef = useRef<Core | null>(null);
  const [keyboardHelpVisible, setKeyboardHelpVisible] = useState(false);
  const focusedNodeIndexRef = useRef(0);

  useEffect(() => {
    if (!containerRef.current) {
      return;
    }

    const { elements } = buildCytoscapeElements(map.nodes, map.edges);

    const theme = readGraphThemeFromDocument();
    if (containerRef.current) {
      containerRef.current.style.background = theme.canvasBackground;
    }

    const cy = cytoscape({
      container: containerRef.current,
      elements,
      style: buildCytoscapeStyles(theme),
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
    const cy = cyRef.current;
    if (!cy) {
      return;
    }

    function applyTheme() {
      const theme = readGraphThemeFromDocument();
      cy!.style(buildCytoscapeStyles(theme));
      if (containerRef.current) {
        containerRef.current.style.background = theme.canvasBackground;
      }
    }

    const observer = new MutationObserver(applyTheme);
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });
    return () => observer.disconnect();
  }, [map]);

  useEffect(() => {
    function onResize() {
      cyRef.current?.resize();
      cyRef.current?.fit(undefined, 40);
    }

    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  function focusNodeByIndex(index: number) {
    const cy = cyRef.current;
    if (!cy) {
      return;
    }

    const nodes = cy.nodes();
    if (nodes.length === 0) {
      return;
    }

    const wrapped = ((index % nodes.length) + nodes.length) % nodes.length;
    focusedNodeIndexRef.current = wrapped;
    const node = nodes[wrapped];
    cy.nodes().unselect();
    node.select();
    cy.center(node);
    onNodeSelected?.(node.id());
  }

  function onCanvasKeyDown(event: React.KeyboardEvent<HTMLDivElement>) {
    const cy = cyRef.current;
    if (!cy || cy.nodes().length === 0) {
      return;
    }

    if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
      event.preventDefault();
      focusNodeByIndex(focusedNodeIndexRef.current + 1);
    } else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
      event.preventDefault();
      focusNodeByIndex(focusedNodeIndexRef.current - 1);
    } else if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      const node = cy.nodes()[focusedNodeIndexRef.current];
      if (node) {
        onNodeSelected?.(node.id());
      }
    } else if (event.key === 'Escape') {
      event.preventDefault();
      cy.nodes().unselect();
      onNodeSelected?.(null);
    } else if (event.key === '?') {
      setKeyboardHelpVisible((visible) => !visible);
    }
  }

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
      {keyboardHelpVisible ? (
        <p className="small text-muted mb-2" role="note">
          Keyboard: arrow keys cycle nodes, Enter selects, Escape clears, ? toggles help.
        </p>
      ) : null}
      <div
        ref={containerRef}
        className="system-map-graph-canvas"
        role="application"
        tabIndex={0}
        aria-label={`System map graph for ${map.applicationName ?? 'application'}. Use arrow keys to navigate nodes.`}
        onKeyDown={onCanvasKeyDown}
        onFocus={() => focusNodeByIndex(focusedNodeIndexRef.current)}
      />
    </section>
  );
}

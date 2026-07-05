import type { Stylesheet } from 'cytoscape';
import type { SystemGraphEdge, SystemGraphNode } from '../types/spa';

const NODE_COLORS: Record<string, string> = {
  Table: '#4e79a7',
  Controller: '#59a14f',
  Entity: '#edc948',
  DbContext: '#76b7b2',
  Service: '#f28e2b',
  ApiEndpoint: '#e15759',
  Repository: '#b07aa1',
};

export function nodeColor(type: string): string {
  return NODE_COLORS[type] ?? '#9c755f';
}

export interface CytoscapeElement {
  data: {
    id: string;
    label?: string;
    type?: string;
    detail?: string;
    source?: string;
    target?: string;
    edgeLabel?: string;
  };
}

const MAX_GRAPH_NODES = 400;

export function buildCytoscapeElements(
  nodes: SystemGraphNode[],
  edges: SystemGraphEdge[],
): { elements: CytoscapeElement[]; truncated: boolean; nodeCount: number; edgeCount: number } {
  const limitedNodes = nodes.slice(0, MAX_GRAPH_NODES);
  const nodeIds = new Set(limitedNodes.map((node) => node.id));
  const limitedEdges = edges.filter((edge) => nodeIds.has(edge.fromId) && nodeIds.has(edge.toId));

  const elements: CytoscapeElement[] = [
    ...limitedNodes.map((node) => ({
      data: {
        id: node.id,
        label: node.label,
        type: node.type,
        detail: node.detail,
      },
    })),
    ...limitedEdges.map((edge) => ({
      data: {
        id: `${edge.fromId}->${edge.toId}:${edge.label}`,
        source: edge.fromId,
        target: edge.toId,
        edgeLabel: edge.label,
      },
    })),
  ];

  return {
    elements,
    truncated: nodes.length > MAX_GRAPH_NODES,
    nodeCount: limitedNodes.length,
    edgeCount: limitedEdges.length,
  };
}

export function buildCytoscapeStyles(): Stylesheet[] {
  return [
    {
      selector: 'node',
      style: {
        label: 'data(label)',
        'text-valign': 'center',
        'text-halign': 'center',
        'font-size': 10,
        'text-wrap': 'wrap',
        'text-max-width': '90px',
        color: '#1f2937',
        'background-color': '#9c755f',
        width: 36,
        height: 36,
      },
    },
    ...Object.entries(NODE_COLORS).map(([type, color]) => ({
      selector: `node[type = "${type}"]`,
      style: { 'background-color': color },
    })),
    {
      selector: 'edge',
      style: {
        width: 1.5,
        'line-color': '#94a3b8',
        'target-arrow-color': '#94a3b8',
        'target-arrow-shape': 'triangle',
        'curve-style': 'bezier',
        label: 'data(edgeLabel)',
        'font-size': 8,
        color: '#64748b',
        'text-rotation': 'autorotate',
      },
    },
    {
      selector: 'node:selected',
      style: {
        'border-width': 3,
        'border-color': '#2563eb',
      },
    },
    {
      selector: 'edge:selected',
      style: {
        'line-color': '#2563eb',
        'target-arrow-color': '#2563eb',
        width: 2.5,
      },
    },
  ] as Stylesheet[];
}

export interface EnhancementRequestDetail {
  id: string;
  title: string;
  status: string;
  businessDescription: string;
  desiredOutcome: string;
  submittedByUserName?: string;
}

export interface EnhancementAnalysis {
  id: string;
  version: number;
  featureSummary?: string;
  confidenceScore: number;
  riskLevel: string;
  affectedApplications?: unknown[];
  databaseChangeRecommendations?: unknown[];
  apiChangeRecommendations?: unknown[];
}

export interface ApplicationSummary {
  id: string;
  name: string;
}

export interface SystemGraphNode {
  id: string;
  label: string;
  type: string;
  detail?: string;
}

export interface SystemGraphEdge {
  fromId: string;
  toId: string;
  label: string;
}

export interface SystemMap {
  applicationId: string;
  applicationName?: string;
  nodes: SystemGraphNode[];
  edges: SystemGraphEdge[];
  builtAt?: string;
}

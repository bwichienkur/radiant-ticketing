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

export interface PendingApprovalItem {
  id: string;
  title: string;
  submittedByUserName?: string;
  priority: string;
  latestRiskLevel?: string;
  daysInStatus?: number;
}

export interface AnalysisSummary {
  id: string;
  version: number;
  featureSummary?: string;
  riskLevel: string;
  confidenceScore: number;
}

export interface ApprovalRequestDetail extends EnhancementRequestDetail {
  priority: string;
  department?: string;
  analyses: AnalysisSummary[];
}

export interface OnboardingSession {
  id: string;
  applicationId?: string;
  applicationName?: string;
  currentStep: string;
  status: string;
  skipDatabase: boolean;
  discoveryJobState: string;
  discoveryStatus?: string;
  lastError?: string;
  wizardError?: string;
  completedAt?: string;
}

export interface OnboardingReview {
  applicationId: string;
  applicationName: string;
  repositoryCount: number;
  databaseConnectionCount: number;
  graphNodeCount: number;
  graphEdgeCount: number;
  driftFindingCount: number;
  profileCount: number;
  latestProfileSummary?: string;
}

export interface RepositoryPathValidation {
  isValid: boolean;
  errorMessage?: string;
  csharpFileCount: number;
  controllerCount: number;
}

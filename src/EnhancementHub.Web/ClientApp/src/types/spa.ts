export interface CommentSummary {
  id: string;
  userDisplayName: string;
  content: string;
  isInternal: boolean;
  createdAt: string;
}

export interface EnhancementRequestDetail {
  id: string;
  title: string;
  status: string;
  businessDescription: string;
  desiredOutcome: string;
  priority?: string;
  supportingNotes?: string;
  targetApplicationId?: string;
  submittedByUserName?: string;
  createdAt?: string;
  comments?: CommentSummary[];
}

export interface AffectedApplicationItem {
  applicationId: string;
  applicationName?: string;
  impactDescription: string;
}

export interface DatabaseChangeItem {
  tableName: string;
  changeType: string;
  description: string;
}

export interface ApiChangeItem {
  endpoint: string;
  changeType: string;
  description: string;
}

export interface EnhancementAnalysis {
  id: string;
  version: number;
  featureSummary?: string;
  confidenceScore: number;
  riskLevel: string;
  riskExplanation?: string;
  technicalRequirements?: string;
  testingPlan?: string;
  needsClarification?: boolean;
  affectedApplications?: AffectedApplicationItem[];
  databaseChangeRecommendations?: DatabaseChangeItem[];
  apiChangeRecommendations?: ApiChangeItem[];
}

export interface ApprovalHistoryItem {
  id: string;
  actionType: string;
  userDisplayName: string;
  comments?: string;
  createdAt: string;
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

export interface GitHubAppStatus {
  isConfigured: boolean;
  defaultInstallationId?: number;
}

export interface OnPremAgentSetup {
  agentId: string;
  connectionId: string;
  connectionName: string;
  apiBaseUrl: string;
  agentConfigSnippet: string;
  runCommand: string;
}

export interface DatabaseConnectionStringResult {
  connectionString: string;
}

export interface RepositoryPathValidation {
  isValid: boolean;
  errorMessage?: string;
  csharpFileCount: number;
  controllerCount: number;
}

export interface PlatformRuntimeStatus {
  aiConfigured: boolean;
  aiProvider: string;
  vectorSearchProvider: string;
  qaRunner: string;
  allowMockInProduction: boolean;
  usesSimulatedBackends: boolean;
}

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

export interface ApplicationListItem {
  id: string;
  name: string;
  businessDomain?: string;
  repositoryCount: number;
}

export interface DatabaseConnectionSummary {
  id: string;
  applicationId: string;
  applicationName?: string;
  name: string;
  scanStatus: string;
}

export interface SchemaDriftFinding {
  id: string;
  severity: string;
  title: string;
  description: string;
  isResolved: boolean;
}

export interface DriftReport {
  connectionId: string;
  detectedAt?: string;
  findings: SchemaDriftFinding[];
}

export interface RepositoryListItem {
  id: string;
  applicationId: string;
  applicationName?: string;
  name: string;
  url: string;
  indexingStatus: string;
  lastIndexedAt?: string;
}

export interface AuditLogEntry {
  id: string;
  action: string;
  entityType: string;
  entityId: string;
  userName?: string;
  comments?: string;
  createdAt: string;
}

export interface AuditLogFilters {
  entityType?: string;
  action?: string;
  from?: string;
  to?: string;
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

export interface EnhancementRequestListItem {
  id: string;
  title: string;
  priority: string;
  status: string | number;
  targetApplicationName?: string;
  submittedByUserName?: string;
  latestRiskLevel?: string | number;
  daysInStatus?: number;
}

export interface PagedEnhancementRequests {
  items: EnhancementRequestListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
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

export interface DashboardActivityItem {
  eventType: string;
  title: string;
  subtitle?: string;
  occurredAt: string;
  entityId?: string;
  linkPath: string;
}

export interface DailyRequestCount {
  date: string;
  count: number;
}

export interface DashboardInsights {
  recentActivity: DashboardActivityItem[];
  requestsLast7Days: DailyRequestCount[];
  myPendingApprovals: number;
  myAwaitingAnalysis: number;
  unresolvedDriftFindings: number;
  staleRepositoryCount: number;
}

export interface DashboardReport {
  totalRequests: number;
  pendingApprovalCount: number;
  awaitingAnalysisCount: number;
  highRiskPendingApprovalCount: number;
  readyForDevelopmentCount: number;
  averageApprovalTimeHours?: number;
}

export interface OnboardingStatus {
  applicationCount: number;
  repositoryCount: number;
  databaseConnectionCount: number;
  hasIndexedRepository: boolean;
  hasSystemGraph: boolean;
  activeSessionId?: string;
}

export interface DashboardPageData {
  report: DashboardReport;
  insights: DashboardInsights;
  onboardingStatus: OnboardingStatus;
  isApprover: boolean;
  showOnboardingChecklist: boolean;
}

export interface EnhancementTemplateSummary {
  id: string;
  name: string;
  domainCategory: string;
  title: string;
  priority: string;
}

export interface EnhancementTemplate extends EnhancementTemplateSummary {
  businessDescription: string;
  desiredOutcome: string;
  supportingNotes?: string;
}

export interface CreateRequestFormData {
  applications: ApplicationSummary[];
  templates: EnhancementTemplateSummary[];
}

export interface CreateRequestInput {
  title: string;
  businessDescription: string;
  desiredOutcome: string;
  priority: string;
  targetApplicationId?: string;
  requestedDueDate?: string;
  department?: string;
  supportingNotes?: string;
  templateId?: string;
}

export interface CreatedRequestSummary {
  id: string;
}

export interface IntakeCopilotMessage {
  role: string;
  content: string;
  occurredAt: string;
}

export interface IntakeCopilotDraft {
  title: string;
  businessDescription: string;
  desiredOutcome: string;
  priority: string;
  targetApplicationId?: string;
  department?: string;
  supportingNotes?: string;
  suggestedTemplateDomainCategory?: string;
}

export interface IntakeCopilotSession {
  id: string;
  status: string;
  turnCount: number;
  messages: IntakeCopilotMessage[];
  draft?: IntakeCopilotDraft;
  suggestedTemplateId?: string;
  createdRequestId?: string;
  lastAssistantMessage?: string;
  policySourceLabel?: string;
  hasPolicySource: boolean;
}

export interface IntakeCopilotTurnResponse {
  session: IntakeCopilotSession;
  assistantMessage: string;
  followUpQuestions: string[];
  isComplete: boolean;
  usedMockAi: boolean;
}

export interface DeliveryTimelineEvent {
  occurredAt: string;
  message: string;
}

export interface QaTestStep {
  step: string;
  passed: boolean;
  detail: string;
}

export interface DeliveryRunTestResult {
  testCaseId: string;
  title: string;
  isRegressionCase: boolean;
  passed: boolean;
  durationMs: number;
  detail?: string;
}

export interface EnhancementDeliveryRun {
  id: string;
  enhancementRequestId: string;
  runNumber: number;
  phase: string;
  isSimulation: boolean;
  branchName?: string;
  pullRequestUrl?: string;
  pullRequestNumber?: number;
  testUrl?: string;
  testDeployReference?: string;
  qaSteps: QaTestStep[];
  testCaseResults: DeliveryRunTestResult[];
  qaPassed?: boolean;
  qaVideoUrl?: string;
  qaReportUrl?: string;
  qaRunner: string;
  qaStartedAt?: string;
  qaFinishedAt?: string;
  uatApproved: boolean;
  uatSignedOffAt?: string;
  uatNotes?: string;
  prodScheduledAt?: string;
  prodDeployReference?: string;
  prodDeployedAt?: string;
  prodArtifactReference?: string;
  rollbackTargetCommitSha?: string;
  postDeploySmokePassed?: boolean;
  rolledBackAt?: string;
  canDeployToProduction: boolean;
  canRollbackProduction: boolean;
  requiresHumanProdDeploy: boolean;
  rollbackPlan?: string;
  timeline: DeliveryTimelineEvent[];
  lastError?: string;
}

export interface GlobalSearchItem {
  type: string;
  title: string;
  subtitle?: string;
  url: string;
  score?: number;
}

export interface GlobalSearchResult {
  query: string;
  items: GlobalSearchItem[];
  groups: Record<string, GlobalSearchItem[]>;
}

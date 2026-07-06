export interface PlatformRuntimeStatus {
  aiConfigured: boolean;
  aiProvider: string;
  vectorSearchProvider: string;
  qaRunner: string;
  allowMockInProduction: boolean;
  usesSimulatedBackends: boolean;
  featureFlags?: Record<string, boolean>;
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
  provider?: string;
  isReadOnly?: boolean;
  scanStatus: string;
  lastScannedAt?: string;
  scanError?: string;
}

export interface DatabaseColumn {
  name: string;
  dataType: string;
  isNullable: boolean;
  isPrimaryKey: boolean;
  isForeignKey: boolean;
  ordinalPosition: number;
}

export interface DatabaseTable {
  id: string;
  schemaName: string;
  tableName: string;
  columns: DatabaseColumn[];
}

export interface DatabaseSchema {
  connectionId: string;
  connectionName: string;
  tables: DatabaseTable[];
  relationships: Array<{
    fromTable: string;
    fromColumn: string;
    toTable: string;
    toColumn: string;
    relationshipType: string;
  }>;
}

export interface ErdDiagram {
  applicationId: string;
  mermaid: string;
}

export type DocumentationExportFormat = 'Markdown' | 'Mermaid' | 'Both';

export interface BlastRadiusItem {
  name: string;
  type: string;
  impact: string;
  depth: number;
}

export interface BlastRadiusResult {
  targetName: string;
  affectedItems: BlastRadiusItem[];
}

export interface RefactorPlanSummary {
  id: string;
  title: string;
  targetDescription: string;
  status: string;
  riskLevel: string;
  confidenceScore: number;
  createdAt: string;
}

export interface RefactorPlanDetail {
  id: string;
  title: string;
  targetDescription: string;
  planMarkdown?: string;
  blastRadius?: BlastRadiusResult;
  status: string;
  createdAt: string;
}

export interface RegisterDatabaseConnectionInput {
  applicationId: string;
  name: string;
  provider: string;
  connectionString: string;
  isReadOnly: boolean;
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

export interface DriftRequestDraft {
  findingId: string;
  title: string;
  businessDescription: string;
  desiredOutcome: string;
  priority: string;
  targetApplicationId?: string;
  supportingNotes?: string;
  databaseConnectionId: string;
  connectionName?: string;
  severity: string;
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

export interface DashboardDriftFinding {
  id: string;
  title: string;
  severity: string;
  connectionName: string;
  databaseConnectionId: string;
  detectedAt: string;
  linkPath: string;
}

export interface DashboardInsights {
  recentActivity: DashboardActivityItem[];
  requestsLast7Days: DailyRequestCount[];
  myPendingApprovals: number;
  myAwaitingAnalysis: number;
  unresolvedDriftFindings: number;
  staleRepositoryCount: number;
  topDriftFindings: DashboardDriftFinding[];
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

export interface ApprovalRecommendation {
  enhancementRequestId: string;
  recommendation: string;
  summary: string;
  riskLevel?: string;
  confidenceScore?: number;
  needsClarification: boolean;
  source?: string;
}

export interface RoiCategoryMetric {
  category: string;
  requestCount: number;
}

export interface RoiReport {
  totalAnalysesCompleted: number;
  averageAnalysisDurationMinutes: number;
  estimatedManualAnalysisHoursPerRequest: number;
  estimatedHoursSaved: number;
  highOrCriticalRiskApprovedCount: number;
  driftFindingsResolved: number;
  driftFindingsTotal: number;
  architectEditsRecorded: number;
  humanApprovedFindings: number;
  aiSuggestedFindings: number;
  templateUsageByCategory: RoiCategoryMetric[];
  averageTimeToAnalysisHours?: number | null;
  averageTimeToApprovalHours?: number | null;
  mockAiRunPercent: number;
  totalAiRunsCompleted: number;
  averagePilotNps?: number | null;
  totalFeedbackSubmissions: number;
}

export interface IntakeQualityScore {
  score: number;
  readyToSubmit: boolean;
  missingFields: string[];
  suggestions: string[];
}

export interface AiBudgetStatus {
  enabled: boolean;
  dailyTokenLimit: number;
  tokensUsedToday: number;
  tokensRemaining: number;
  dailyCostLimitUsd: number;
  costUsedTodayUsd: number;
  costRemainingUsd: number;
}

export interface AnalysisFieldChange {
  fieldName: string;
  valueA?: string;
  valueB?: string;
  changed: boolean;
}

export interface AnalysisComparison {
  enhancementRequestId: string;
  versionA: number;
  versionB: number;
  riskLevelA: string;
  riskLevelB: string;
  confidenceScoreA: number;
  confidenceScoreB: number;
  fieldChanges: AnalysisFieldChange[];
  architectEditsBetweenVersions: number;
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
  customFields: CustomFieldDefinition[];
}

export interface CustomFieldDefinition {
  id: string;
  key: string;
  label: string;
  fieldType: string;
  isRequired: boolean;
  isActive: boolean;
  sortOrder: number;
  options: string[];
}

export interface CustomFieldValueInput {
  key: string;
  textValue?: string;
  numberValue?: number;
  dateValue?: string;
  userValueId?: string;
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
  customFields?: CustomFieldValueInput[];
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

export interface AuthenticationConfigurationStatus {
  openIdConnectEnabled: boolean;
  isProductionReady: boolean;
  authority?: string;
  clientId?: string;
  clientSecretConfigured: boolean;
  defaultRole?: string;
  scopes: string[];
  roleMappings: Array<{
    source: string;
    targetRole: string;
    isValidTargetRole: boolean;
    isGuidFormat: boolean;
  }>;
  issues: Array<{
    severity: string;
    message: string;
  }>;
}

export interface SystemSetting {
  id: string;
  key: string;
  value: string;
  category: string;
  description?: string;
}

export interface TeamSummary {
  id: string;
  name: string;
  description?: string;
  memberCount: number;
  applicationCount: number;
}

export interface ServiceApiKeySummary {
  id: string;
  name: string;
  description?: string;
  keyPrefix: string;
  role: string;
  isActive: boolean;
  expiresAt?: string;
  lastUsedAt?: string;
  createdAt: string;
}

export interface CreateServiceApiKeyResult {
  id: string;
  name: string;
  apiKey: string;
  keyPrefix: string;
  role: string;
  expiresAt?: string;
}

export interface WebhookSubscriptionSummary {
  id: string;
  name: string;
  url: string;
  secretPrefix: string;
  eventTypes: string;
  isActive: boolean;
  createdAt: string;
  lastDeliveryAt?: string;
  failedDeliveryCount: number;
}

export interface CreateWebhookSubscriptionResult {
  id: string;
  name: string;
  secret: string;
  secretPrefix: string;
  eventTypes: string;
}

export interface WebhookDeliverySummary {
  id: string;
  webhookSubscriptionId: string;
  subscriptionName: string;
  eventType: string;
  status: string;
  attemptCount: number;
  httpStatusCode?: number;
  lastError?: string;
  createdAt: string;
  deliveredAt?: string;
}

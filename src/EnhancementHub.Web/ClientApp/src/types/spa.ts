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

export interface ApplicationDetailItem {
  id: string;
  name: string;
  businessDomain?: string;
  purpose?: string;
  description?: string;
  ownerTeamId: string;
  riskSensitiveAreas?: string;
  repositoryCount: number;
}

export interface ApplicationProfile {
  id: string;
  applicationId: string;
  repositoryId: string;
  purpose?: string;
  businessDomain?: string;
  keyComponents?: string;
  databaseUsage?: string;
  externalIntegrations?: string;
  internalDependencies?: string;
  deploymentNotes?: string;
  riskSensitiveAreas?: string;
  ownershipMetadata?: string;
  generatedAt: string;
}

export interface ApplicationDetailResponse {
  application: ApplicationDetailItem;
  profiles: ApplicationProfile[];
}

export interface NotificationPreference {
  type: number;
  label: string;
  emailEnabled: boolean;
  inAppEnabled: boolean;
}

export interface UpdateNotificationPreferenceInput {
  type: number;
  emailEnabled: boolean;
  inAppEnabled: boolean;
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
  fieldType: string | number;
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
  semanticHint?: string | null;
}

export interface PortfolioApplicationHealth {
  applicationId: string;
  applicationName: string;
  unresolvedDriftCount: number;
  pendingRequestCount: number;
  highRiskPendingCount: number;
  staleRepositoryCount: number;
  riskScore: number;
}

export interface PortfolioHealthReport {
  applications: PortfolioApplicationHealth[];
  generatedAtUtc: string;
}

export interface TenantBranding {
  logoUrl?: string | null;
  accentColor: string;
  productName?: string | null;
}

export interface UserAppearance {
  themePreference: string;
  branding: TenantBranding;
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

export interface AdminJobsResponse {
  status: BackgroundJobsStatus;
  freshness: IndexFreshnessReport;
}

export interface BackgroundJobsStatus {
  provider: string;
  generatedAtUtc: string;
  queueCounts: {
    pendingRepositoryIndexing: number;
    awaitingAiAnalysis: number;
    queuedApplicationDiscovery: number;
    pendingDatabaseSchemaScans: number;
  };
  jobs: Array<{
    jobId: string;
    description: string;
    schedule: string;
    lastExecution?: string;
    nextExecution?: string;
  }>;
  hangfire?: {
    enqueued: number;
    processing: number;
    scheduled: number;
    succeeded: number;
    failed: number;
    deleted: number;
  };
  failedJobs: Array<{
    jobId: string;
    jobName?: string;
    failedAt?: string;
    exceptionMessage?: string;
  }>;
}

export interface IndexFreshnessReport {
  slaHours: number;
  totalRepositories: number;
  freshCount: number;
  staleCount: number;
  neverIndexedCount: number;
  inProgressCount: number;
  failedCount: number;
  freshnessPercent: number;
  slaMet: boolean;
  generatedAtUtc: string;
  staleRepositories: Array<{
    id: string;
    name: string;
    applicationId: string;
    applicationName?: string;
    lastIndexedAt?: string;
    hoursSinceIndexed?: number;
    indexingStatus: string;
  }>;
}

export interface Soc2ReadinessReport {
  implementedCount: number;
  partialCount: number;
  gapCount: number;
  controls: Array<{
    controlId: string;
    trustServiceCategory: string;
    title: string;
    enhancementHubFeature: string;
    status: string;
    configurationHint?: string;
  }>;
}

export interface AdminComplianceResponse {
  report: Soc2ReadinessReport;
  runtimeStatus: PlatformRuntimeStatus;
}

export interface TenantBilling {
  tenantId: string;
  tenantName: string;
  plan: string;
  region: string;
  isTrialActive: boolean;
  isTrialExpired: boolean;
  trialEndsAt?: string;
  subscriptionStatus: string;
  subscriptionPeriodEnd?: string;
  hasActiveSubscription: boolean;
  stripeEnabled: boolean;
  maxApplications: number;
  maxAnalysesPerMonth: number;
  maxStorageMegabytes: number;
  applicationCount: number;
  analysisCountThisMonth: number;
  storageBytes: number;
  isOverLimit: boolean;
}

export interface TenantIsolation {
  tenantId: string;
  isolationMode: string;
  databaseSchemaName?: string;
  isSchemaProvisioned: boolean;
  schemaProvisionedAt?: string;
  isolationEnabled: boolean;
}

export interface TenantSummary {
  id: string;
  name: string;
  slug: string;
  plan: string;
  region: string;
  isActive: boolean;
  trialEndsAt?: string;
}

export interface AdminTenancyResponse {
  billing?: TenantBilling;
  isolation?: TenantIsolation;
  allTenants: TenantSummary[];
}

export interface ObservabilityStatus {
  generatedAt: string;
  openTelemetry: {
    enabled: boolean;
    serviceName: string;
    otlpEndpoint?: string;
    prometheusMetricsEnabled: boolean;
    backgroundJobInstrumentationEnabled: boolean;
    activeInstrumentations: string[];
  };
  dataProtection: {
    storageProvider: string;
    sharedKeyRingConfigured: boolean;
    keysPath?: string;
    azureBlobContainer?: string;
    issues: Array<{ severity: string; message: string }>;
  };
  highAvailability: {
    postgresConfigured: boolean;
    hangfireConfigured: boolean;
    readReplicaConfigured: boolean;
    vectorOffloadConfigured: boolean;
    sharedDataProtectionConfigured: boolean;
    observabilityEnabled: boolean;
    recommendations: string[];
  };
}

export interface DataScalingStatus {
  generatedAtUtc: string;
  vectorSearch: {
    provider: string;
    isProductionReady: boolean;
    recommendedProvider?: string;
    dimensions: number;
    issues: Array<{ severity: string; message: string }>;
  };
  database: {
    readReplicaConfigured: boolean;
    primaryConnectionName: string;
    reportingConnectionName: string;
    maxPoolSize: number;
    schemaScanMaxConcurrency: number;
    databaseProvider: string;
  };
  archival: {
    auditLogCount: number;
    aiPromptRunCount: number;
    eligibleAiPromptRunArchiveCount: number;
    archiveBeforeDeleteEnabled: boolean;
    archivePath?: string;
    aiPromptRunsRetentionDays: number;
    recommendations: string[];
  };
}

export interface DataRetentionStatus {
  enabled: boolean;
  aiPromptRunsRetentionDays: number;
  attachmentsRetentionDays: number;
  batchSize: number;
  eligibleAiPromptRunCount: number;
  eligibleAttachmentCount: number;
  aiPromptRunsCutoffUtc?: string;
  attachmentsCutoffUtc?: string;
}

export interface DataRetentionResult {
  dryRun: boolean;
  aiPromptRunsDeleted: number;
  retrievedContextItemsDeleted: number;
  attachmentsDeleted: number;
  attachmentFilesDeleted: number;
  appliedAtUtc: string;
}

export interface AiPromptConfiguration {
  id: string;
  name: string;
  version: string;
  systemPromptTemplate: string;
  userPromptTemplate: string;
  isActive: boolean;
}

export interface TenantDeliveryProfile {
  id: string;
  defaultCicdProvider: number;
  vaultSecretPrefix?: string;
  autoImplementOnApprove: boolean;
  autoDeployToTest: boolean;
  requirePullRequestReview: boolean;
  requireUatSignoff: boolean;
  requireProdChangeWindow: boolean;
  changeWindowNotes?: string;
  qaVideoRetentionDays: number;
  allowOneClickProdDeploy: boolean;
  allowOneClickRollback: boolean;
  testDataStrategy: number;
  allowProdToTestRefresh: boolean;
  environments: TenantDeploymentEnvironment[];
}

export interface TenantDeploymentEnvironment {
  id: string;
  name: string;
  environmentType: number;
  baseUrlTemplate?: string;
  secretReferencePrefix?: string;
  isActive: boolean;
  sortOrder: number;
  requiresApprovalForDeploy: boolean;
}

export interface DeliveryProfileValidation {
  isValid: boolean;
  errors: string[];
  warnings: string[];
}

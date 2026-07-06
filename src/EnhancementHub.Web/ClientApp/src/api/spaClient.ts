import type {
  AiBudgetStatus,
  AnalysisComparison,
  ApplicationListItem,
  ApprovalRecommendation,
  IntakeQualityScore,
  AuditLogEntry,
  AuditLogFilters,
  DatabaseConnectionSummary,
  DatabaseSchema,
  DriftReport,
  DriftRequestDraft,
  ApprovalHistoryItem,
  ApprovalRequestDetail,
  CreateRequestFormData,
  CreateRequestInput,
  CreatedRequestSummary,
  DashboardPageData,
  DatabaseConnectionStringResult,
  EnhancementDeliveryRun,
  EnhancementAnalysis,
  EnhancementRequestDetail,
  PagedEnhancementRequests,
  EnhancementTemplate,
  IntakeCopilotSession,
  IntakeCopilotTurnResponse,
  GitHubAppStatus,
  OnboardingReview,
  OnboardingSession,
  OnPremAgentSetup,
  PendingApprovalItem,
  PlatformRuntimeStatus,
  RepositoryPathValidation,
  RepositoryListItem,
  ErdDiagram,
  BlastRadiusResult,
  RefactorPlanSummary,
  RefactorPlanDetail,
  RegisterDatabaseConnectionInput,
  DocumentationExportFormat,
  AuthenticationConfigurationStatus,
  SystemSetting,
  TeamSummary,
  ServiceApiKeySummary,
  CreateServiceApiKeyResult,
  WebhookSubscriptionSummary,
  CreateWebhookSubscriptionResult,
  WebhookDeliverySummary,
  GlobalSearchItem,
  GlobalSearchResult,
  SystemMap,
} from '../types/spa';

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url, { credentials: 'include' });
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }

  return response.json() as Promise<T>;
}

async function postJson<T>(url: string, body?: unknown): Promise<T> {
  const response = await fetch(url, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!response.ok) {
    const errorBody = (await response.json().catch(() => null)) as { message?: string } | null;
    throw new Error(errorBody?.message ?? `Request failed: ${response.status}`);
  }

  return response.json() as Promise<T>;
}

async function putJson<T>(url: string, body?: unknown): Promise<T> {
  const response = await fetch(url, {
    method: 'PUT',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!response.ok) {
    const errorBody = (await response.json().catch(() => null)) as { message?: string } | null;
    throw new Error(errorBody?.message ?? `Request failed: ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export async function getRequestDetail(requestId: string): Promise<EnhancementRequestDetail> {
  return fetchJson<EnhancementRequestDetail>(`/web-api/spa/requests/${requestId}`);
}

export async function getRequestAnalysis(requestId: string): Promise<EnhancementAnalysis | null> {
  const response = await fetch(`/web-api/spa/analysis/${requestId}`, { credentials: 'include' });
  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`Analysis request failed: ${response.status}`);
  }

  return response.json() as Promise<EnhancementAnalysis>;
}

export async function getApprovalHistory(requestId: string): Promise<ApprovalHistoryItem[]> {
  return fetchJson<ApprovalHistoryItem[]>(`/web-api/spa/requests/${requestId}/approval-history`);
}

export async function postRequestComment(
  requestId: string,
  content: string,
  isInternal = false,
): Promise<void> {
  await postJson(`/web-api/spa/requests/${requestId}/comments`, { content, isInternal });
}

export async function listApplications(): Promise<ApplicationListItem[]> {
  return fetchJson<ApplicationListItem[]>('/web-api/spa/applications');
}

export async function listDatabaseConnections(): Promise<DatabaseConnectionSummary[]> {
  return fetchJson<DatabaseConnectionSummary[]>('/web-api/spa/connections');
}

export async function registerDatabaseConnection(
  input: RegisterDatabaseConnectionInput,
): Promise<DatabaseConnectionSummary> {
  return postJson<DatabaseConnectionSummary>('/web-api/spa/connections', input);
}

export async function triggerDatabaseScan(connectionId: string): Promise<DatabaseConnectionSummary> {
  return postJson<DatabaseConnectionSummary>(`/web-api/spa/connections/${connectionId}/scan`);
}

export async function getDatabaseSchema(connectionId: string): Promise<DatabaseSchema> {
  return fetchJson<DatabaseSchema>(`/web-api/spa/connections/${connectionId}/schema`);
}

export async function getConnectionErd(connectionId: string): Promise<ErdDiagram> {
  return fetchJson<ErdDiagram>(`/web-api/spa/connections/${connectionId}/erd`);
}

export function exportDocumentation(applicationId: string, format: DocumentationExportFormat): void {
  const params = new URLSearchParams();
  params.set('applicationId', applicationId);
  params.set('format', format);
  window.location.assign(`/web-api/spa/documentation/export?${params.toString()}`);
}

export async function analyzeRefactorBlastRadius(
  applicationId: string,
  target: string,
): Promise<BlastRadiusResult> {
  return postJson<BlastRadiusResult>('/web-api/spa/refactor/analyze', { applicationId, target });
}

export async function generateRefactorPlan(
  applicationId: string,
  target: string,
): Promise<RefactorPlanDetail> {
  return postJson<RefactorPlanDetail>('/web-api/spa/refactor/plans', { applicationId, target });
}

export async function listRefactorPlans(): Promise<RefactorPlanSummary[]> {
  return fetchJson<RefactorPlanSummary[]>('/web-api/spa/refactor/plans');
}

export async function getRefactorPlan(planId: string): Promise<RefactorPlanDetail> {
  return fetchJson<RefactorPlanDetail>(`/web-api/spa/refactor/plans/${planId}`);
}

export async function getAuthenticationConfigurationStatus(): Promise<AuthenticationConfigurationStatus> {
  return fetchJson<AuthenticationConfigurationStatus>('/web-api/spa/settings/authentication');
}

export async function listSystemSettings(): Promise<SystemSetting[]> {
  return fetchJson<SystemSetting[]>('/web-api/spa/settings/system');
}

export async function updateSystemSetting(settingId: string, value: string): Promise<void> {
  await putJson(`/web-api/spa/settings/system/${settingId}`, { value });
}

export async function listAdminTeams(): Promise<TeamSummary[]> {
  return fetchJson<TeamSummary[]>('/web-api/spa/settings/teams');
}

export async function createAdminTeam(name: string, description?: string): Promise<TeamSummary> {
  return postJson<TeamSummary>('/web-api/spa/settings/teams', { name, description });
}

export async function listServiceApiKeys(): Promise<ServiceApiKeySummary[]> {
  return fetchJson<ServiceApiKeySummary[]>('/web-api/spa/settings/api-keys');
}

export async function createServiceApiKey(input: {
  name: string;
  description?: string;
  role: string;
  teamId?: string;
  expiresInDays?: number;
}): Promise<CreateServiceApiKeyResult> {
  return postJson<CreateServiceApiKeyResult>('/web-api/spa/settings/api-keys', input);
}

export async function revokeServiceApiKey(keyId: string): Promise<void> {
  await postJson(`/web-api/spa/settings/api-keys/${keyId}/revoke`);
}

export async function listWebhookEventTypes(): Promise<string[]> {
  const result = await fetchJson<{ eventTypes: string[] }>('/web-api/spa/settings/webhooks');
  return result.eventTypes;
}

export async function listWebhookSubscriptions(): Promise<WebhookSubscriptionSummary[]> {
  return fetchJson<WebhookSubscriptionSummary[]>('/web-api/spa/settings/webhooks/subscriptions');
}

export async function listWebhookDeliveries(): Promise<WebhookDeliverySummary[]> {
  return fetchJson<WebhookDeliverySummary[]>('/web-api/spa/settings/webhooks/deliveries');
}

export async function createWebhookSubscription(input: {
  name: string;
  url: string;
  eventTypes: string[];
}): Promise<CreateWebhookSubscriptionResult> {
  return postJson<CreateWebhookSubscriptionResult>('/web-api/spa/settings/webhooks/subscriptions', input);
}

export async function revokeWebhookSubscription(subscriptionId: string): Promise<void> {
  await postJson(`/web-api/spa/settings/webhooks/subscriptions/${subscriptionId}/revoke`);
}

export async function listDriftConnections(): Promise<DatabaseConnectionSummary[]> {
  return fetchJson<DatabaseConnectionSummary[]>('/web-api/spa/drift/connections');
}

export async function getDriftReport(connectionId: string): Promise<DriftReport> {
  return fetchJson<DriftReport>(`/web-api/spa/drift/report?connectionId=${encodeURIComponent(connectionId)}`);
}

export async function getDriftRequestDraft(findingId: string): Promise<DriftRequestDraft> {
  return fetchJson<DriftRequestDraft>(
    `/web-api/spa/drift/request-draft?findingId=${encodeURIComponent(findingId)}`,
  );
}

export async function detectSchemaDrift(connectionId: string): Promise<DriftReport> {
  return postJson<DriftReport>('/web-api/spa/drift/detect', { connectionId });
}

export async function listRepositoriesCatalog(): Promise<RepositoryListItem[]> {
  return fetchJson<RepositoryListItem[]>('/web-api/spa/repositories');
}

export async function triggerRepositoryReindex(repositoryId: string): Promise<void> {
  await postJson(`/web-api/spa/repositories/${repositoryId}/reindex`);
}

export async function listAuditLogs(filters: AuditLogFilters = {}): Promise<AuditLogEntry[]> {
  const params = new URLSearchParams();
  if (filters.entityType?.trim()) {
    params.set('entityType', filters.entityType.trim());
  }
  if (filters.action?.trim()) {
    params.set('action', filters.action.trim());
  }
  if (filters.from) {
    params.set('from', filters.from);
  }
  if (filters.to) {
    params.set('to', filters.to);
  }
  const query = params.toString();
  return fetchJson<AuditLogEntry[]>(`/web-api/spa/audit/logs${query ? `?${query}` : ''}`);
}

export function exportAuditLogs(format: 'csv' | 'json', filters: AuditLogFilters = {}): void {
  const params = new URLSearchParams();
  params.set('format', format);
  if (filters.entityType?.trim()) {
    params.set('entityType', filters.entityType.trim());
  }
  if (filters.action?.trim()) {
    params.set('action', filters.action.trim());
  }
  if (filters.from) {
    params.set('from', filters.from);
  }
  if (filters.to) {
    params.set('to', filters.to);
  }
  window.location.assign(`/web-api/spa/audit/export?${params.toString()}`);
}

export async function getSystemMap(applicationId: string): Promise<SystemMap> {
  return fetchJson<SystemMap>(`/web-api/spa/system-map/${applicationId}`);
}

export async function rebuildSystemMap(applicationId: string): Promise<SystemMap> {
  return postJson<SystemMap>(`/web-api/spa/system-map/${applicationId}/rebuild`);
}

export async function listPendingApprovals(): Promise<PendingApprovalItem[]> {
  return fetchJson<PendingApprovalItem[]>('/web-api/spa/approvals/pending');
}

export async function getApprovalRequestDetail(requestId: string): Promise<ApprovalRequestDetail> {
  return fetchJson<ApprovalRequestDetail>(`/web-api/spa/requests/${requestId}`);
}

export async function getApprovalRecommendation(requestId: string): Promise<ApprovalRecommendation> {
  return fetchJson<ApprovalRecommendation>(`/web-api/spa/approvals/${requestId}/recommendation`);
}

export async function submitApprovalAction(
  requestId: string,
  actionType: string,
  comments?: string,
): Promise<void> {
  await postJson(`/web-api/spa/approvals/${requestId}/action`, { actionType, comments });
}

export interface BulkApprovalItemResult {
  requestId: string;
  success: boolean;
  errorMessage?: string;
}

export interface BulkApprovalActionResult {
  succeededCount: number;
  failedCount: number;
  results: BulkApprovalItemResult[];
}

export async function bulkSubmitApprovalActions(
  requestIds: string[],
  actionType: 'Approve' | 'Reject',
  comments?: string,
): Promise<BulkApprovalActionResult> {
  return postJson<BulkApprovalActionResult>('/web-api/spa/approvals/bulk-action', {
    requestIds,
    actionType,
    comments,
  });
}

export async function startOnboardingSession(): Promise<OnboardingSession> {
  return postJson<OnboardingSession>('/web-api/spa/onboarding/start');
}

export async function getOnboardingSession(sessionId: string): Promise<OnboardingSession> {
  return fetchJson<OnboardingSession>(`/web-api/spa/onboarding/${sessionId}`);
}

export async function getOnboardingReview(sessionId: string): Promise<OnboardingReview> {
  return fetchJson<OnboardingReview>(`/web-api/spa/onboarding/${sessionId}/review`);
}

export async function validateRepositoryPath(path: string): Promise<RepositoryPathValidation> {
  return postJson<RepositoryPathValidation>('/web-api/spa/onboarding/validate-path', { path });
}

export async function submitOnboardingBasics(
  sessionId: string,
  payload: {
    name: string;
    businessDomain?: string;
    purpose?: string;
    riskSensitiveAreas?: string;
    ownerTeamName?: string;
    deploymentNotes?: string;
  },
): Promise<OnboardingSession> {
  return postJson<OnboardingSession>(`/web-api/spa/onboarding/${sessionId}/basics`, payload);
}

export async function submitOnboardingRepository(
  sessionId: string,
  payload: { repositoryName: string; repositoryPath: string; defaultBranch: string },
): Promise<OnboardingSession> {
  return postJson<OnboardingSession>(`/web-api/spa/onboarding/${sessionId}/repository`, payload);
}

export async function submitOnboardingDatabase(
  sessionId: string,
  payload: {
    connectionName: string;
    provider: string;
    connectionString: string;
    isReadOnly: boolean;
  },
): Promise<OnboardingSession> {
  return postJson<OnboardingSession>(`/web-api/spa/onboarding/${sessionId}/database`, payload);
}

export async function skipOnboardingDatabase(sessionId: string): Promise<OnboardingSession> {
  return postJson<OnboardingSession>(`/web-api/spa/onboarding/${sessionId}/skip-database`);
}

export async function queueOnboardingDiscovery(sessionId: string): Promise<OnboardingSession> {
  return postJson<OnboardingSession>(`/web-api/spa/onboarding/${sessionId}/discovery`);
}

export async function completeOnboarding(sessionId: string): Promise<OnboardingSession> {
  return postJson<OnboardingSession>(`/web-api/spa/onboarding/${sessionId}/complete`);
}

export async function advanceOnboardingToReview(sessionId: string): Promise<OnboardingSession> {
  return postJson<OnboardingSession>(`/web-api/spa/onboarding/${sessionId}/advance-review`);
}

export async function getGitHubAppStatus(): Promise<GitHubAppStatus> {
  return fetchJson<GitHubAppStatus>('/web-api/spa/onboarding/github-app/status');
}

export async function uploadOnboardingZip(
  sessionId: string,
  file: File,
  repositoryName?: string,
): Promise<OnboardingSession> {
  const form = new FormData();
  form.append('zipFile', file);
  if (repositoryName) {
    form.append('repositoryName', repositoryName);
  }

  const response = await fetch(`/web-api/spa/onboarding/${sessionId}/upload-zip`, {
    method: 'POST',
    credentials: 'include',
    body: form,
  });

  if (!response.ok) {
    const errorBody = (await response.json().catch(() => null)) as { message?: string } | null;
    throw new Error(errorBody?.message ?? `Upload failed: ${response.status}`);
  }

  return response.json() as Promise<OnboardingSession>;
}

export async function cloneGitHubAppRepository(
  sessionId: string,
  payload: {
    repositoryName: string;
    owner: string;
    repository: string;
    defaultBranch: string;
    installationId?: number;
  },
): Promise<OnboardingSession> {
  return postJson<OnboardingSession>(`/web-api/spa/onboarding/${sessionId}/clone-github-app`, payload);
}

export async function cloneGitRepository(
  sessionId: string,
  payload: {
    repositoryName: string;
    repositoryUrl: string;
    defaultBranch: string;
    accessToken?: string;
  },
): Promise<OnboardingSession> {
  return postJson<OnboardingSession>(`/web-api/spa/onboarding/${sessionId}/clone-git`, payload);
}

export async function buildDatabaseConnectionString(payload: {
  provider: string;
  host: string;
  port: number;
  database: string;
  username?: string;
  password?: string;
  integratedSecurity?: boolean;
}): Promise<DatabaseConnectionStringResult> {
  return postJson<DatabaseConnectionStringResult>('/web-api/spa/onboarding/build-connection-string', payload);
}

export async function setupOnPremAgent(
  sessionId: string,
  payload: { applicationId: string; connectionName: string; provider: string },
): Promise<OnPremAgentSetup> {
  return postJson<OnPremAgentSetup>(`/web-api/spa/onboarding/${sessionId}/on-prem-agent`, payload);
}

export function getOnboardingExportDocsUrl(sessionId: string): string {
  return `/web-api/spa/onboarding/${sessionId}/export-docs`;
}

export async function getDashboard(): Promise<DashboardPageData> {
  return fetchJson('/web-api/spa/dashboard');
}

export async function searchGlobal(query: string, limit = 20): Promise<GlobalSearchItem[]> {
  return fetchJson<GlobalSearchItem[]>(
    `/web-api/spa/search?q=${encodeURIComponent(query)}&limit=${limit}`,
  );
}

export async function searchGlobalGrouped(query: string, limit = 20): Promise<GlobalSearchResult> {
  return fetchJson<GlobalSearchResult>(
    `/web-api/spa/search?q=${encodeURIComponent(query)}&grouped=true&limit=${limit}`,
  );
}

export async function searchPipeline(query: string): Promise<
  Array<{ type?: string; title: string; subtitle?: string; url: string }>
> {
  const response = await fetch(`/web-api/ux/copilot?q=${encodeURIComponent(query)}`, {
    credentials: 'include',
  });
  if (!response.ok) {
    throw new Error(`Search failed: ${response.status}`);
  }
  const data = (await response.json()) as { items?: unknown[] };
  return (data.items ?? data) as Array<{ type?: string; title: string; subtitle?: string; url: string }>;
}

export async function getCreateRequestForm(): Promise<CreateRequestFormData> {
  return fetchJson('/web-api/spa/requests/create-form');
}

export async function getEnhancementTemplate(templateId: string): Promise<EnhancementTemplate> {
  return fetchJson(`/web-api/spa/templates/${templateId}`);
}

export async function createEnhancementRequest(input: CreateRequestInput): Promise<CreatedRequestSummary> {
  return postJson('/web-api/spa/requests', input);
}

export async function startIntakeCopilotSession(initialPrompt?: string): Promise<IntakeCopilotTurnResponse> {
  return postJson('/web-api/spa/intake/sessions', initialPrompt ? { initialPrompt } : {});
}

export async function getIntakeCopilotSession(sessionId: string): Promise<IntakeCopilotSession> {
  return fetchJson(`/web-api/spa/intake/sessions/${sessionId}`);
}

export async function sendIntakeCopilotMessage(
  sessionId: string,
  message: string,
): Promise<IntakeCopilotTurnResponse> {
  return postJson(`/web-api/spa/intake/sessions/${sessionId}/messages`, { message });
}

export async function attachIntakePolicyDocument(
  sessionId: string,
  file: File,
): Promise<IntakeCopilotTurnResponse> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await fetch(`/web-api/spa/intake/sessions/${sessionId}/policy-document`, {
    method: 'POST',
    credentials: 'include',
    body: formData,
  });

  if (!response.ok) {
    const errorBody = (await response.json().catch(() => null)) as { message?: string } | null;
    throw new Error(errorBody?.message ?? `Request failed: ${response.status}`);
  }

  return response.json() as Promise<IntakeCopilotTurnResponse>;
}

export async function attachIntakePolicyUrl(
  sessionId: string,
  url: string,
): Promise<IntakeCopilotTurnResponse> {
  return postJson(`/web-api/spa/intake/sessions/${sessionId}/policy-url`, { url });
}

export async function createRequestFromIntakeSession(
  sessionId: string,
  overrides?: CreateRequestInput,
): Promise<CreatedRequestSummary> {
  return postJson(
    `/web-api/spa/intake/sessions/${sessionId}/create-request`,
    overrides ? { overrides } : {},
  );
}

export async function getIntakeCopilotBudget(): Promise<AiBudgetStatus> {
  return fetchJson<AiBudgetStatus>('/web-api/spa/intake/budget');
}

export async function scoreIntakeDraft(draft: {
  title?: string;
  businessDescription?: string;
  desiredOutcome?: string;
  priority?: string;
  targetApplicationId?: string;
  department?: string;
  supportingNotes?: string;
}): Promise<IntakeQualityScore> {
  return postJson<IntakeQualityScore>('/web-api/spa/intake/score-draft', {
    ...draft,
    targetApplicationId: draft.targetApplicationId || undefined,
  });
}

export async function getAnalysisEvolution(requestId: string): Promise<AnalysisComparison> {
  return fetchJson<AnalysisComparison>(`/web-api/spa/analysis/${requestId}/evolution`);
}

export async function listEnhancementRequests(params: {
  q?: string;
  status?: string;
  priority?: string;
  view?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}): Promise<PagedEnhancementRequests> {
  const search = new URLSearchParams();
  if (params.q) {
    search.set('q', params.q);
  }
  if (params.status) {
    search.set('status', params.status);
  }
  if (params.priority) {
    search.set('priority', params.priority);
  }
  if (params.view) {
    search.set('view', params.view);
  }
  if (params.sort) {
    search.set('sort', params.sort);
  }
  if (params.page) {
    search.set('page', String(params.page));
  }
  if (params.pageSize) {
    search.set('pageSize', String(params.pageSize));
  }

  const query = search.toString();
  return fetchJson<PagedEnhancementRequests>(
    `/web-api/spa/requests${query ? `?${query}` : ''}`,
  );
}

export async function exportEnhancementRequests(ids: string[]): Promise<void> {
  const response = await fetch('/web-api/spa/requests/export', {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ids }),
  });

  if (!response.ok) {
    throw new Error('Export failed.');
  }

  const blob = await response.blob();
  const disposition = response.headers.get('Content-Disposition');
  const filename =
    disposition?.match(/filename="?([^";]+)"?/)?.[1] ?? 'enhancement-requests.csv';
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}

export async function getDeliveryRun(requestId: string): Promise<EnhancementDeliveryRun | null> {
  const response = await fetch(`/web-api/spa/delivery/requests/${requestId}/run`, {
    credentials: 'include',
  });
  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`Delivery run request failed: ${response.status}`);
  }

  return response.json() as Promise<EnhancementDeliveryRun>;
}

export async function startDeliveryRun(requestId: string): Promise<EnhancementDeliveryRun> {
  return postJson<EnhancementDeliveryRun>(`/web-api/spa/delivery/requests/${requestId}/start`);
}

export async function advanceDeliveryPastPr(requestId: string): Promise<EnhancementDeliveryRun> {
  return postJson<EnhancementDeliveryRun>(`/web-api/spa/delivery/requests/${requestId}/advance-pr`);
}

export async function signUat(
  requestId: string,
  approved: boolean,
  notes?: string,
): Promise<EnhancementDeliveryRun> {
  return postJson<EnhancementDeliveryRun>(`/web-api/spa/delivery/requests/${requestId}/uat`, {
    approved,
    notes,
  });
}

export async function deployProduction(requestId: string): Promise<EnhancementDeliveryRun> {
  return postJson<EnhancementDeliveryRun>(`/web-api/spa/delivery/requests/${requestId}/deploy-production`);
}

export async function rollbackProduction(
  requestId: string,
  reason?: string,
): Promise<EnhancementDeliveryRun> {
  return postJson<EnhancementDeliveryRun>(`/web-api/spa/delivery/requests/${requestId}/rollback-production`, {
    reason,
  });
}

export async function getPlatformRuntimeStatus(): Promise<PlatformRuntimeStatus> {
  return fetchJson<PlatformRuntimeStatus>('/web-api/spa/platform/runtime-status');
}

export interface SubmitProductFeedbackInput {
  workflowKey: string;
  npsScore: number;
  comment?: string;
}

export async function submitProductFeedback(
  input: SubmitProductFeedbackInput,
): Promise<{ id: string; workflowKey: string; npsScore: number }> {
  return postJson('/web-api/spa/feedback', input);
}

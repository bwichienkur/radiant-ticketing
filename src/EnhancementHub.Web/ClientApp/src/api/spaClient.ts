import type {
  ApplicationSummary,
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
  EnhancementRequestListItem,
  EnhancementTemplate,
  IntakeCopilotSession,
  IntakeCopilotTurnResponse,
  GitHubAppStatus,
  OnboardingReview,
  OnboardingSession,
  OnPremAgentSetup,
  PendingApprovalItem,
  RepositoryPathValidation,
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

export async function listApplications(): Promise<ApplicationSummary[]> {
  return fetchJson<ApplicationSummary[]>('/web-api/spa/applications');
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

export async function submitApprovalAction(
  requestId: string,
  actionType: string,
  comments?: string,
): Promise<void> {
  await postJson(`/web-api/spa/approvals/${requestId}/action`, { actionType, comments });
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

export async function listEnhancementRequests(params: {
  q?: string;
  status?: string;
  priority?: string;
  view?: string;
  sort?: string;
}): Promise<EnhancementRequestListItem[]> {
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

  const query = search.toString();
  return fetchJson<EnhancementRequestListItem[]>(
    `/web-api/spa/requests${query ? `?${query}` : ''}`,
  );
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

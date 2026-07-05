import type {
  ApplicationSummary,
  ApprovalRequestDetail,
  EnhancementAnalysis,
  EnhancementRequestDetail,
  OnboardingReview,
  OnboardingSession,
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

export async function listApplications(): Promise<ApplicationSummary[]> {
  return fetchJson<ApplicationSummary[]>('/web-api/spa/applications');
}

export async function getSystemMap(applicationId: string): Promise<SystemMap> {
  return fetchJson<SystemMap>(`/web-api/spa/system-map/${applicationId}`);
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

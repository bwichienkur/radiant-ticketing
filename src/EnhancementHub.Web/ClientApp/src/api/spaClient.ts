import type {
  ApplicationSummary,
  EnhancementAnalysis,
  EnhancementRequestDetail,
  SystemMap,
} from '../types/spa';

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url, { credentials: 'include' });
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
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

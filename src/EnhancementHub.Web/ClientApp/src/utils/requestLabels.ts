const STATUS_BY_VALUE: Record<number, string> = {
  0: 'Submitted',
  1: 'AiAnalyzing',
  2: 'NeedsClarification',
  3: 'PendingApproval',
  4: 'Approved',
  5: 'Rejected',
  6: 'ReadyForDevelopment',
  7: 'InProgress',
  8: 'Completed',
  9: 'Cancelled',
};

const STATUS_LABELS: Record<string, string> = {
  Submitted: 'Submitted — waiting for review',
  AiAnalyzing: 'Being analyzed',
  Analyzing: 'Being analyzed',
  NeedsClarification: 'More information needed',
  PendingApproval: 'Waiting for approval',
  Approved: 'Approved',
  Rejected: 'Not approved',
  ReadyForDevelopment: 'Approved — ready for development',
  InProgress: 'In progress',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
};

const STATUS_NEXT_STEP: Record<string, string> = {
  Submitted: 'We are reviewing your request. This page updates automatically.',
  AiAnalyzing: 'We are reviewing your request. This page updates automatically.',
  Analyzing: 'We are reviewing your request. This page updates automatically.',
  NeedsClarification: 'An approver needs more detail from you. Check comments below.',
  PendingApproval: 'Waiting for an approver to make a decision.',
  Approved: 'Approved. Your IT team can schedule the work.',
  Rejected: 'This request was not approved. See approval history for comments.',
  ReadyForDevelopment: 'Approved. Your IT team can schedule the work.',
  InProgress: 'Work on this change is underway.',
  Completed: 'This change has been completed.',
  Cancelled: 'This request was cancelled.',
};

const APPROVAL_ACTION_LABELS: Record<string, string> = {
  Approve: 'Approved',
  Reject: 'Rejected',
  RequestClarification: 'Asked for more information',
};

export function normalizeRequestStatus(status: string | number): string {
  if (typeof status === 'number') {
    return STATUS_BY_VALUE[status] ?? String(status);
  }

  if (/^\d+$/.test(status)) {
    return STATUS_BY_VALUE[Number(status)] ?? status;
  }

  return status;
}

export function formatRequestStatus(status: string | number): string {
  const normalized = normalizeRequestStatus(status);
  return STATUS_LABELS[normalized] ?? normalized.replace(/([a-z])([A-Z])/g, '$1 $2');
}

export function getStatusNextStep(status: string | number): string {
  const normalized = normalizeRequestStatus(status);
  return STATUS_NEXT_STEP[normalized] ?? 'Track progress here or contact your approver if you have questions.';
}

export function formatApprovalAction(actionType: string): string {
  return APPROVAL_ACTION_LABELS[actionType] ?? actionType.replace(/([a-z])([A-Z])/g, '$1 $2');
}

export function formatConfidenceLabel(score: number): string {
  const percent = Math.round(score * 100);
  return `${percent}% confident`;
}

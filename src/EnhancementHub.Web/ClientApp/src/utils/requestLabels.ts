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
  10: 'Implementing',
  11: 'InTest',
  12: 'QaInProgress',
  13: 'AwaitingUat',
  14: 'UatApproved',
  15: 'ProdScheduled',
  16: 'DeployingToProduction',
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
  Implementing: 'Building the change',
  InTest: 'Deployed to test',
  QaInProgress: 'Running automated QA',
  AwaitingUat: 'Waiting for your acceptance',
  UatApproved: 'Accepted — scheduling production',
  ProdScheduled: 'Production deploy scheduled',
  DeployingToProduction: 'Deploying to production',
  RolledBack: 'Production rolled back',
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
  Implementing: 'We are creating a branch and pull request for this change.',
  InTest: 'The change is live in your test environment.',
  QaInProgress: 'Automated tests are running against the test environment.',
  AwaitingUat: 'Please review the test environment and approve when ready.',
  UatApproved: 'You approved the change. Production deploy is being scheduled.',
  ProdScheduled: 'Production deploy is scheduled for the next change window.',
  DeployingToProduction: 'Production deployment is in progress.',
  RolledBack: 'Production was rolled back to the previous version.',
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

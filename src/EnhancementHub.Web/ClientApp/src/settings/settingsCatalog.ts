export type SettingsCategoryId =
  | 'workspace'
  | 'authentication'
  | 'teams'
  | 'developer'
  | 'ai'
  | 'security'
  | 'compliance'
  | 'observability'
  | 'data'
  | 'billing'
  | 'integrations'
  | 'notifications'
  | 'branding';

export type SettingsGroupId = 'workspace' | 'developer' | 'platform' | 'security' | 'ai';

export interface SettingsSectionDef {
  id: string;
  route: string;
  title: string;
  description: string;
  categoryId: SettingsCategoryId;
  groupId: SettingsGroupId;
  keywords: string[];
  settingCount: number;
  docHint?: string;
  relatedIds?: string[];
}

export interface SettingsCategoryDef {
  id: SettingsCategoryId;
  title: string;
  description: string;
  icon: string;
  groupId: SettingsGroupId;
}

export interface SettingsGroupDef {
  id: SettingsGroupId;
  title: string;
  description: string;
}

export const SETTINGS_GROUPS: SettingsGroupDef[] = [
  { id: 'workspace', title: 'Workspace', description: 'General settings, branding, and notifications' },
  { id: 'developer', title: 'Developer', description: 'API keys, webhooks, and custom fields' },
  { id: 'platform', title: 'Platform', description: 'Jobs, scaling, delivery, and retention' },
  { id: 'security', title: 'Security', description: 'Authentication, compliance, and audit logs' },
  { id: 'ai', title: 'AI', description: 'Models, prompts, and knowledge configuration' },
];

export const SETTINGS_CATEGORIES: SettingsCategoryDef[] = [
  { id: 'workspace', title: 'Workspace', description: 'General configuration and workspace defaults', icon: 'workspace', groupId: 'workspace' },
  { id: 'authentication', title: 'Authentication', description: 'SSO, OIDC, Entra ID, and role mappings', icon: 'authentication', groupId: 'security' },
  { id: 'teams', title: 'Team Management', description: 'Teams, members, and access boundaries', icon: 'teams', groupId: 'workspace' },
  { id: 'developer', title: 'Developer Platform', description: 'API keys, webhooks, and extensibility', icon: 'api', groupId: 'developer' },
  { id: 'ai', title: 'AI Configuration', description: 'Prompts, models, and analysis workflows', icon: 'ai', groupId: 'ai' },
  { id: 'security', title: 'Security', description: 'Identity hardening and audit visibility', icon: 'security', groupId: 'security' },
  { id: 'compliance', title: 'Compliance', description: 'SOC 2 readiness and control mapping', icon: 'compliance', groupId: 'security' },
  { id: 'observability', title: 'Observability', description: 'Telemetry, monitoring, and HA posture', icon: 'monitoring', groupId: 'platform' },
  { id: 'data', title: 'Data Management', description: 'Scaling, retention, and archival policies', icon: 'database', groupId: 'platform' },
  { id: 'billing', title: 'Billing', description: 'Plans, usage limits, and subscription management', icon: 'billing', groupId: 'workspace' },
  { id: 'integrations', title: 'Integrations', description: 'Webhooks and outbound event delivery', icon: 'integrations', groupId: 'developer' },
  { id: 'notifications', title: 'Notifications', description: 'Email, in-app, and delivery preferences', icon: 'notifications', groupId: 'workspace' },
  { id: 'branding', title: 'Branding', description: 'Product name, accent color, and logo', icon: 'branding', groupId: 'workspace' },
];

export const SETTINGS_SECTIONS: SettingsSectionDef[] = [
  {
    id: 'general',
    route: '/Spa/Settings/General',
    title: 'General',
    description: 'Core workspace settings, feature flags, and system configuration values.',
    categoryId: 'workspace',
    groupId: 'workspace',
    keywords: ['system', 'configuration', 'defaults', 'feature', 'general'],
    settingCount: 12,
    docHint: 'Edit key/value pairs that control platform behavior.',
    relatedIds: ['branding', 'authentication'],
  },
  {
    id: 'branding',
    route: '/Spa/Settings/Branding',
    title: 'Branding',
    description: 'White-label your workspace with product name, accent color, and logo.',
    categoryId: 'branding',
    groupId: 'workspace',
    keywords: ['logo', 'accent', 'color', 'product name', 'white label'],
    settingCount: 3,
    relatedIds: ['general'],
  },
  {
    id: 'notifications',
    route: '/Spa/Account/Notifications',
    title: 'Notifications',
    description: 'Configure email and in-app notification preferences for your account.',
    categoryId: 'notifications',
    groupId: 'workspace',
    keywords: ['email', 'alerts', 'preferences', 'notify'],
    settingCount: 8,
    relatedIds: ['webhooks'],
  },
  {
    id: 'authentication',
    route: '/Spa/Settings/Authentication',
    title: 'Authentication',
    description: 'OpenID Connect, Entra ID, client credentials, and role mapping validation.',
    categoryId: 'authentication',
    groupId: 'security',
    keywords: ['sso', 'oidc', 'entra', 'openid', 'roles', 'identity'],
    settingCount: 6,
    docHint: 'Review OIDC configuration and role mapping health.',
    relatedIds: ['compliance', 'api-keys'],
  },
  {
    id: 'teams',
    route: '/Spa/Settings/Teams',
    title: 'Teams',
    description: 'Create teams, assign members, and scope applications to organizational units.',
    categoryId: 'teams',
    groupId: 'workspace',
    keywords: ['members', 'groups', 'organization', 'access'],
    settingCount: 4,
    relatedIds: ['api-keys'],
  },
  {
    id: 'api-keys',
    route: '/Spa/Settings/ApiKeys',
    title: 'API Keys',
    description: 'Machine-to-machine credentials for integrations and automation.',
    categoryId: 'developer',
    groupId: 'developer',
    keywords: ['m2m', 'token', 'service account', 'api', 'credentials'],
    settingCount: 5,
    relatedIds: ['webhooks', 'authentication'],
  },
  {
    id: 'webhooks',
    route: '/Spa/Settings/Webhooks',
    title: 'Webhooks',
    description: 'Outbound event subscriptions and delivery monitoring.',
    categoryId: 'integrations',
    groupId: 'developer',
    keywords: ['events', 'callback', 'subscription', 'delivery', 'http'],
    settingCount: 7,
    relatedIds: ['api-keys', 'notifications'],
  },
  {
    id: 'custom-fields',
    route: '/Spa/Admin/CustomFields',
    title: 'Custom Fields',
    description: 'Extend request forms with tenant-specific field definitions.',
    categoryId: 'developer',
    groupId: 'developer',
    keywords: ['schema', 'fields', 'forms', 'metadata'],
    settingCount: 4,
    relatedIds: ['general'],
  },
  {
    id: 'ai-prompts',
    route: '/Spa/Admin/AiPrompts',
    title: 'AI Prompts',
    description: 'System and user prompt templates for impact analysis workflows.',
    categoryId: 'ai',
    groupId: 'ai',
    keywords: ['prompt', 'llm', 'analysis', 'template', 'model'],
    settingCount: 6,
    relatedIds: ['compliance', 'delivery'],
  },
  {
    id: 'jobs',
    route: '/Spa/Admin/Jobs',
    title: 'Background Jobs',
    description: 'Index freshness, job queue health, and retry controls.',
    categoryId: 'observability',
    groupId: 'platform',
    keywords: ['queue', 'index', 'retry', 'background', 'worker'],
    settingCount: 5,
    relatedIds: ['observability', 'data-scaling'],
  },
  {
    id: 'delivery',
    route: '/Spa/Admin/Delivery',
    title: 'Delivery',
    description: 'Deployment environments and tenant delivery profiles.',
    categoryId: 'data',
    groupId: 'platform',
    keywords: ['deploy', 'environment', 'release', 'pipeline'],
    settingCount: 4,
    relatedIds: ['jobs'],
  },
  {
    id: 'data-scaling',
    route: '/Spa/Admin/DataScaling',
    title: 'Data Scaling',
    description: 'Vector search, read replicas, pooling, and archival configuration.',
    categoryId: 'data',
    groupId: 'platform',
    keywords: ['vector', 'replica', 'pool', 'archive', 'scale'],
    settingCount: 5,
    relatedIds: ['retention', 'observability'],
  },
  {
    id: 'retention',
    route: '/Spa/Admin/Retention',
    title: 'Retention',
    description: 'Data retention policies, purge windows, and compliance holds.',
    categoryId: 'data',
    groupId: 'platform',
    keywords: ['purge', 'retention', 'archive', 'delete', 'gdpr'],
    settingCount: 4,
    relatedIds: ['compliance', 'data-scaling'],
  },
  {
    id: 'compliance',
    route: '/Spa/Admin/Compliance',
    title: 'Compliance',
    description: 'SOC 2 readiness report, control mapping, and runtime posture.',
    categoryId: 'compliance',
    groupId: 'security',
    keywords: ['soc2', 'audit', 'controls', 'gdpr', 'policy'],
    settingCount: 8,
    relatedIds: ['authentication', 'retention'],
  },
  {
    id: 'audit',
    route: '/Spa/Audit',
    title: 'Audit Logs',
    description: 'Immutable activity trail across workspace and platform events.',
    categoryId: 'security',
    groupId: 'security',
    keywords: ['audit', 'logs', 'activity', 'trail', 'events'],
    settingCount: 3,
    relatedIds: ['compliance'],
  },
  {
    id: 'observability',
    route: '/Spa/Admin/Observability',
    title: 'Observability',
    description: 'OpenTelemetry, metrics exporters, and high-availability checklist.',
    categoryId: 'observability',
    groupId: 'platform',
    keywords: ['otel', 'metrics', 'prometheus', 'telemetry', 'ha'],
    settingCount: 6,
    relatedIds: ['jobs', 'compliance'],
  },
  {
    id: 'tenancy',
    route: '/Spa/Admin/Tenancy',
    title: 'Billing & Tenancy',
    description: 'Subscription plan, usage limits, Stripe billing, and tenant isolation.',
    categoryId: 'billing',
    groupId: 'workspace',
    keywords: ['plan', 'stripe', 'subscription', 'trial', 'usage', 'billing'],
    settingCount: 9,
    relatedIds: ['data-scaling'],
  },
];

export function getSectionByRoute(pathname: string): SettingsSectionDef | undefined {
  return SETTINGS_SECTIONS.find(
    (s) => pathname === s.route || pathname.startsWith(`${s.route}/`),
  );
}

export function getSectionById(id: string): SettingsSectionDef | undefined {
  return SETTINGS_SECTIONS.find((s) => s.id === id);
}

export function getCategoryById(id: SettingsCategoryId): SettingsCategoryDef | undefined {
  return SETTINGS_CATEGORIES.find((c) => c.id === id);
}

export function getSectionsForCategory(categoryId: SettingsCategoryId): SettingsSectionDef[] {
  return SETTINGS_SECTIONS.filter((s) => s.categoryId === categoryId);
}

export function getSectionsForGroup(groupId: SettingsGroupId): SettingsSectionDef[] {
  return SETTINGS_SECTIONS.filter((s) => s.groupId === groupId);
}

export function getCategorySettingCount(categoryId: SettingsCategoryId): number {
  return getSectionsForCategory(categoryId).reduce((sum, s) => sum + s.settingCount, 0);
}

export function searchSettings(query: string): SettingsSectionDef[] {
  const q = query.trim().toLowerCase();
  if (!q) {
    return [];
  }
  return SETTINGS_SECTIONS.filter(
    (s) =>
      s.title.toLowerCase().includes(q) ||
      s.description.toLowerCase().includes(q) ||
      s.keywords.some((k) => k.includes(q)) ||
      SETTINGS_CATEGORIES.find((c) => c.id === s.categoryId)?.title.toLowerCase().includes(q),
  );
}

export const AI_SUGGESTIONS = [
  { label: 'Configure SSO', prompt: 'Help me configure OpenID Connect SSO for my workspace', route: '/Spa/Settings/Authentication' },
  { label: 'Enable audit logging', prompt: 'How do I enable and review audit logging?', route: '/Spa/Audit' },
  { label: 'Secure my workspace', prompt: 'Recommend security settings for an enterprise deployment', route: '/Spa/Admin/Compliance' },
  { label: 'Review compliance', prompt: 'What compliance controls should I review first?', route: '/Spa/Admin/Compliance' },
  { label: 'API key best practices', prompt: 'Best practices for API key rotation and scopes', route: '/Spa/Settings/ApiKeys' },
  { label: 'Explain retention', prompt: 'Explain data retention policies and purge options', route: '/Spa/Admin/Retention' },
] as const;

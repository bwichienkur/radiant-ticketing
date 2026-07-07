export interface SpaPageMeta {
  title: string;
  breadcrumb: string;
  section?: string;
}

const EXACT: Record<string, SpaPageMeta> = {
  '/': { title: 'Dashboard', breadcrumb: 'Dashboard', section: 'Home' },
  '/Index': { title: 'Dashboard', breadcrumb: 'Dashboard', section: 'Home' },
  '/Spa/RequestList': { title: 'Requests', breadcrumb: 'Requests', section: 'Work' },
  '/Spa/CreateRequest': { title: 'New request', breadcrumb: 'New request', section: 'Work' },
  '/Spa/ApprovalQueue': { title: 'Approvals', breadcrumb: 'Approvals', section: 'Work' },
  '/Spa/OnboardingWizard': { title: 'Onboarding', breadcrumb: 'Onboarding', section: 'Work' },
  '/Spa/Portfolio': { title: 'Portfolio', breadcrumb: 'Portfolio', section: 'Portfolio' },
  '/Spa/Applications': { title: 'Applications', breadcrumb: 'Applications', section: 'Portfolio' },
  '/Spa/SystemMap': { title: 'System map', breadcrumb: 'System map', section: 'Portfolio' },
  '/Spa/Repositories': { title: 'Repositories', breadcrumb: 'Repositories', section: 'Portfolio' },
  '/Spa/DatabaseConnections': { title: 'Databases', breadcrumb: 'Databases', section: 'Portfolio' },
  '/Spa/SchemaDrift': { title: 'Schema drift', breadcrumb: 'Schema drift', section: 'Portfolio' },
  '/Spa/Documentation/Export': { title: 'Documentation', breadcrumb: 'Documentation', section: 'Portfolio' },
  '/Spa/Refactor/Analyze': { title: 'Refactor analysis', breadcrumb: 'Refactor', section: 'Portfolio' },
  '/Spa/Refactor/Plans': { title: 'Refactor plans', breadcrumb: 'Refactor plans', section: 'Portfolio' },
  '/Spa/Search': { title: 'Search', breadcrumb: 'Search', section: 'Governance' },
  '/Spa/Audit': { title: 'Audit log', breadcrumb: 'Audit log', section: 'Governance' },
  '/Spa/Insights': { title: 'Insights', breadcrumb: 'Insights', section: 'Governance' },
  '/Spa/PortfolioHealth': { title: 'Portfolio health', breadcrumb: 'Portfolio health', section: 'Governance' },
  '/Spa/Account/Notifications': {
    title: 'Notifications',
    breadcrumb: 'Notifications',
    section: 'Account',
  },
  '/Spa/Settings': { title: 'Settings', breadcrumb: 'Settings', section: 'Settings' },
  '/Spa/Settings/General': { title: 'General settings', breadcrumb: 'General', section: 'Settings' },
  '/Spa/Settings/Authentication': {
    title: 'Authentication',
    breadcrumb: 'Authentication',
    section: 'Settings',
  },
  '/Spa/Settings/ApiKeys': { title: 'API keys', breadcrumb: 'API keys', section: 'Settings' },
  '/Spa/Settings/Teams': { title: 'Teams', breadcrumb: 'Teams', section: 'Settings' },
  '/Spa/Settings/Webhooks': { title: 'Webhooks', breadcrumb: 'Webhooks', section: 'Settings' },
  '/Spa/Settings/Branding': { title: 'Branding', breadcrumb: 'Branding', section: 'Settings' },
  '/Spa/Admin': { title: 'Platform admin', breadcrumb: 'Platform admin', section: 'Settings' },
  '/Spa/Admin/Jobs': { title: 'Background jobs', breadcrumb: 'Jobs', section: 'Settings' },
  '/Spa/Admin/Compliance': { title: 'Compliance', breadcrumb: 'Compliance', section: 'Settings' },
  '/Spa/Admin/CustomFields': { title: 'Custom fields', breadcrumb: 'Custom fields', section: 'Settings' },
  '/Spa/Admin/Tenancy': { title: 'Tenancy & billing', breadcrumb: 'Tenancy', section: 'Settings' },
  '/Spa/Admin/Observability': { title: 'Observability', breadcrumb: 'Observability', section: 'Settings' },
  '/Spa/Admin/DataScaling': { title: 'Data scaling', breadcrumb: 'Data scaling', section: 'Settings' },
  '/Spa/Admin/Retention': { title: 'Retention', breadcrumb: 'Retention', section: 'Settings' },
  '/Spa/Admin/Delivery': { title: 'Delivery', breadcrumb: 'Delivery', section: 'Settings' },
  '/Spa/Admin/AiPrompts': { title: 'AI prompts', breadcrumb: 'AI prompts', section: 'Settings' },
};

const PREFIXES: Array<{ prefix: string; meta: SpaPageMeta }> = [
  { prefix: '/Spa/RequestDetail/', meta: { title: 'Request', breadcrumb: 'Request', section: 'Work' } },
  { prefix: '/Spa/Applications/', meta: { title: 'Application', breadcrumb: 'Application', section: 'Portfolio' } },
  {
    prefix: '/Spa/DatabaseConnections/',
    meta: { title: 'Database', breadcrumb: 'Database', section: 'Portfolio' },
  },
  { prefix: '/Spa/OnboardingWizard/', meta: { title: 'Onboarding', breadcrumb: 'Onboarding', section: 'Work' } },
  { prefix: '/Spa/ApprovalQueue/', meta: { title: 'Approvals', breadcrumb: 'Approvals', section: 'Work' } },
];

export function resolveSpaPageMeta(pathname: string): SpaPageMeta {
  const path = pathname.split('?')[0] ?? pathname;
  if (EXACT[path]) {
    return EXACT[path];
  }

  for (const entry of PREFIXES) {
    if (path.startsWith(entry.prefix)) {
      return entry.meta;
    }
  }

  if (path.startsWith('/Spa/Settings/')) {
    return { title: 'Settings', breadcrumb: 'Settings', section: 'Settings' };
  }

  if (path.startsWith('/Spa/Admin/')) {
    return { title: 'Platform admin', breadcrumb: 'Platform admin', section: 'Settings' };
  }

  return { title: 'EnhancementHub', breadcrumb: 'Page', section: 'Home' };
}

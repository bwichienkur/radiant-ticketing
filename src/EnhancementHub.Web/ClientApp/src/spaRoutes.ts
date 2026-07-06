/** Client-side routes served by the React SPA shell (Razor layout unchanged). */
export const SPA_EXACT_PATHS = new Set(['/', '/Index']);

export const SPA_PREFIXES = [
  '/Spa/RequestList',
  '/Spa/CreateRequest',
  '/Spa/RequestDetail',
  '/Spa/ApprovalQueue',
  '/Spa/OnboardingWizard',
  '/Spa/SystemMap',
  '/Spa/Applications',
  '/Spa/SchemaDrift',
  '/Spa/Repositories',
  '/Spa/Audit',
  '/Spa/Search',
];

export function isSpaPath(pathname: string): boolean {
  const path = pathname.split('?')[0];
  if (SPA_EXACT_PATHS.has(path)) {
    return true;
  }

  return SPA_PREFIXES.some((prefix) => path === prefix || path.startsWith(`${prefix}/`));
}

export function readSpaContext(): { isApprover: boolean; isAdmin: boolean } {
  const root = document.getElementById('spa-root');
  return {
    isApprover: root?.dataset.isApprover === 'true',
    isAdmin: root?.dataset.isAdmin === 'true',
  };
}

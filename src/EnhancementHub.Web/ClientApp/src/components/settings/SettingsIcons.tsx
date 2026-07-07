import type { ReactNode, SVGProps } from 'react';

type IconProps = SVGProps<SVGSVGElement>;

function Base({ children, ...props }: IconProps & { children: ReactNode }) {
  return (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" aria-hidden="true" {...props}>
      {children}
    </svg>
  );
}

export function IconWorkspace(props: IconProps) {
  return (
    <Base {...props}>
      <rect x="3" y="3" width="7" height="7" rx="1.5" />
      <rect x="14" y="3" width="7" height="7" rx="1.5" />
      <rect x="3" y="14" width="7" height="7" rx="1.5" />
      <rect x="14" y="14" width="7" height="7" rx="1.5" />
    </Base>
  );
}

export function IconAuthentication(props: IconProps) {
  return (
    <Base {...props}>
      <rect x="5" y="11" width="14" height="10" rx="2" />
      <path d="M8 11V8a4 4 0 0 1 8 0v3" />
      <circle cx="12" cy="16" r="1" fill="currentColor" stroke="none" />
    </Base>
  );
}

export function IconTeams(props: IconProps) {
  return (
    <Base {...props}>
      <circle cx="9" cy="8" r="3" />
      <circle cx="17" cy="9" r="2.5" />
      <path d="M3 19c0-2.8 2.7-5 6-5s6 2.2 6 5" />
      <path d="M15 19c0-1.8 1.3-3.5 4-3.5" />
    </Base>
  );
}

export function IconApi(props: IconProps) {
  return (
    <Base {...props}>
      <path d="M8 9h8M8 15h8" />
      <rect x="4" y="4" width="16" height="16" rx="2" />
      <path d="M9 4v16" />
    </Base>
  );
}

export function IconAi(props: IconProps) {
  return (
    <Base {...props}>
      <path d="M12 3l1.2 3.6L17 8l-3.8 1.2L12 13l-1.2-3.8L7 8l3.8-1.2L12 3z" />
      <path d="M5 18l.8 2.4L8 21l-2.2.7L5 24l-.8-2.3L2 21l2.2-.6L5 18z" transform="scale(0.7) translate(14 2)" />
      <circle cx="12" cy="17" r="3" />
    </Base>
  );
}

export function IconSecurity(props: IconProps) {
  return (
    <Base {...props}>
      <path d="M12 3l8 4v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V7l8-4z" />
      <path d="M9 12l2 2 4-4" />
    </Base>
  );
}

export function IconCompliance(props: IconProps) {
  return (
    <Base {...props}>
      <path d="M9 3h6l1 2h3a1 1 0 0 1 1 1v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a1 1 0 0 1 1-1h3l1-2z" />
      <path d="M9 12l2 2 4-5" />
    </Base>
  );
}

export function IconMonitoring(props: IconProps) {
  return (
    <Base {...props}>
      <path d="M3 3v18h18" />
      <path d="M7 14l3-3 3 2 5-6" />
    </Base>
  );
}

export function IconDatabase(props: IconProps) {
  return (
    <Base {...props}>
      <ellipse cx="12" cy="6" rx="8" ry="3" />
      <path d="M4 6v12c0 1.7 3.6 3 8 3s8-1.3 8-3V6" />
      <path d="M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3" />
    </Base>
  );
}

export function IconBilling(props: IconProps) {
  return (
    <Base {...props}>
      <rect x="3" y="6" width="18" height="12" rx="2" />
      <path d="M3 10h18" />
      <path d="M7 15h4" />
    </Base>
  );
}

export function IconIntegrations(props: IconProps) {
  return (
    <Base {...props}>
      <circle cx="6" cy="12" r="2" />
      <circle cx="18" cy="6" r="2" />
      <circle cx="18" cy="18" r="2" />
      <path d="M8 12h5M15 7.5l2 2M15 16.5l2-2" />
    </Base>
  );
}

export function IconNotifications(props: IconProps) {
  return (
    <Base {...props}>
      <path d="M18 16v-5a6 6 0 1 0-12 0v5l-2 2h16l-2-2z" />
      <path d="M10 20a2 2 0 0 0 4 0" />
    </Base>
  );
}

export function IconBranding(props: IconProps) {
  return (
    <Base {...props}>
      <circle cx="12" cy="12" r="9" />
      <path d="M8 15c1.5-3 6.5-3 8 0" />
      <circle cx="9.5" cy="10" r="1" fill="currentColor" stroke="none" />
      <circle cx="14.5" cy="10" r="1" fill="currentColor" stroke="none" />
    </Base>
  );
}

export function IconChevronRight(props: IconProps) {
  return (
    <Base {...props}>
      <path d="m9 6 6 6-6 6" />
    </Base>
  );
}

export function IconSearch(props: IconProps) {
  return (
    <Base {...props}>
      <circle cx="11" cy="11" r="6" />
      <path d="m20 20-4-4" />
    </Base>
  );
}

export function IconSparkle(props: IconProps) {
  return (
    <Base {...props}>
      <path d="M12 2l1.5 4.5L18 8l-4.5 1.5L12 14l-1.5-4.5L6 8l4.5-1.5L12 2z" />
    </Base>
  );
}

export function IconStar(props: IconProps) {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true" {...props}>
      <path d="M12 2l2.9 6.1L22 9.2l-5 4.4 1.5 6.5L12 17.2 5.5 20.1 7 13.6 2 9.2l7.1-1.1L12 2z" />
    </svg>
  );
}

const ICON_MAP = {
  workspace: IconWorkspace,
  authentication: IconAuthentication,
  teams: IconTeams,
  api: IconApi,
  ai: IconAi,
  security: IconSecurity,
  compliance: IconCompliance,
  monitoring: IconMonitoring,
  database: IconDatabase,
  billing: IconBilling,
  integrations: IconIntegrations,
  notifications: IconNotifications,
  branding: IconBranding,
} as const;

export function SettingsCategoryIcon({ name, className }: { name: string; className?: string }) {
  const Icon = ICON_MAP[name as keyof typeof ICON_MAP] ?? IconWorkspace;
  return <Icon className={className} />;
}

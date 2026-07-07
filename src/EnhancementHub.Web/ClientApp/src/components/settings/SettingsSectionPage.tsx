import type { ReactNode } from 'react';
import { getSectionById } from '../../settings/settingsCatalog';
import { SettingsSectionShell } from './SettingsSectionShell';

export function SettingsSectionPage({
  sectionId,
  children,
}: {
  sectionId: string;
  children: ReactNode;
}) {
  const section = getSectionById(sectionId);
  if (!section) {
    return <>{children}</>;
  }
  return <SettingsSectionShell section={section}>{children}</SettingsSectionShell>;
}

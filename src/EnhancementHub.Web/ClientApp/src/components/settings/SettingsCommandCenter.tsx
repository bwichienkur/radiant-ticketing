import { type ReactNode, useEffect, useMemo, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { getSectionByRoute } from '../../settings/settingsCatalog';
import { SettingsAiAssistant } from './SettingsAiAssistant';
import { SettingsPageHeader } from './SettingsPageHeader';
import { SettingsSidebar } from './SettingsSidebar';
import { SettingsUtilityBar } from './SettingsUtilityBar';
import { SettingsUiProvider } from './SettingsUiContext';

interface SettingsCommandCenterProps {
  children: ReactNode;
}

function resolveMode(pathname: string): 'hub' | 'section' {
  if (pathname === '/Spa/Settings' || pathname.startsWith('/Spa/Settings/Category/')) {
    return 'hub';
  }
  return 'section';
}

export function SettingsCommandCenter({ children }: SettingsCommandCenterProps) {
  const location = useLocation();
  const mode = useMemo(() => resolveMode(location.pathname), [location.pathname]);
  const [aiOpen, setAiOpen] = useState(false);
  const section = mode === 'section' ? getSectionByRoute(location.pathname) : undefined;

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === '/' && !event.metaKey && !event.ctrlKey) {
        const target = event.target as HTMLElement;
        if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA') {
          return;
        }
        event.preventDefault();
        document.querySelector<HTMLInputElement>('.eh-settings-utility-search')?.focus();
      }
    }
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, []);

  return (
    <SettingsUiProvider openAi={() => setAiOpen(true)}>
      <div className={`eh-settings-command-center eh-settings-command-center--${mode}`}>
        {mode === 'section' ? (
          <div className="eh-settings-command-center__top eh-settings-command-center__top--sticky">
            <SettingsPageHeader compact />
            <SettingsUtilityBar onOpenAi={() => setAiOpen(true)} />
          </div>
        ) : null}

        <div
          className={`eh-settings-command-center__body ${mode === 'section' ? 'eh-settings-command-center__body--with-sidebar' : ''}`.trim()}
        >
          {mode === 'section' ? <SettingsSidebar /> : null}
          <main className="eh-settings-command-center__main" id="settings-main" data-section={section?.id}>
            {children}
          </main>
        </div>

        <SettingsAiAssistant open={aiOpen} onClose={() => setAiOpen(false)} />
      </div>
    </SettingsUiProvider>
  );
}

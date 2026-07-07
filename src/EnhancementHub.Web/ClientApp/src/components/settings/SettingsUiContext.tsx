import { createContext, type ReactNode, useContext } from 'react';

interface SettingsUiContextValue {
  openAi: () => void;
}

const SettingsUiContext = createContext<SettingsUiContextValue>({ openAi: () => undefined });

export function useSettingsUi() {
  return useContext(SettingsUiContext);
}

export function SettingsUiProvider({
  openAi,
  children,
}: {
  openAi: () => void;
  children: ReactNode;
}) {
  return <SettingsUiContext.Provider value={{ openAi }}>{children}</SettingsUiContext.Provider>;
}

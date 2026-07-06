import type { ReactNode } from 'react';

interface SpUiRootProps {
  children: ReactNode;
}

export function SpUiRoot({ children }: SpUiRootProps) {
  return (
    <div className="eh-spa-root" data-eh-spa-root>
      <div id="eh-spa-live-region" className="visually-hidden" aria-live="polite" aria-atomic="true" />
      {children}
    </div>
  );
}

import type { ReactNode } from 'react';

interface SectionCardProps {
  title: string;
  children: ReactNode;
  actions?: ReactNode;
  className?: string;
  id?: string;
  ariaLabel?: string;
}

export function SectionCard({
  title,
  children,
  actions,
  className = '',
  id,
  ariaLabel,
}: SectionCardProps) {
  return (
    <section
      className={`card-panel eh-section-card p-4 mb-3 ${className}`.trim()}
      id={id}
      aria-label={ariaLabel}
    >
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <h2 className="eh-section-title mb-0">{title}</h2>
        {actions}
      </div>
      {children}
    </section>
  );
}

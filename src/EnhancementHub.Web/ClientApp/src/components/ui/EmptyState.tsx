import type { ReactNode } from 'react';

type EmptyStateIcon = 'inbox' | 'search' | 'document';

interface EmptyStateProps {
  title: string;
  description?: string;
  action?: ReactNode;
  icon?: EmptyStateIcon;
  embedded?: boolean;
}

const iconClass: Record<EmptyStateIcon, string> = {
  inbox: 'empty-state-graphic-inbox',
  search: 'empty-state-graphic-search',
  document: 'empty-state-graphic-document',
};

export function EmptyState({ title, description, action, icon = 'inbox', embedded = false }: EmptyStateProps) {
  const rootClass = embedded
    ? 'empty-state eh-empty-state embedded-empty-state'
    : 'card-panel empty-state eh-empty-state';

  return (
    <div className={rootClass} role="status">
      <div className={`empty-state-graphic ${iconClass[icon]}`} aria-hidden="true" />
      <h2 className="empty-state-title">{title}</h2>
      {description ? <p className="empty-state-description">{description}</p> : null}
      {action ? <div className="empty-state-action">{action}</div> : null}
    </div>
  );
}

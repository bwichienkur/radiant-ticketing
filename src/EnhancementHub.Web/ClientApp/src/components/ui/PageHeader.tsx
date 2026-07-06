import type { ReactNode } from 'react';

interface PageHeaderProps {
  title: string;
  description?: ReactNode;
  actions?: ReactNode;
  tourId?: string;
  titleAs?: 'h1' | 'h2';
}

export function PageHeader({
  title,
  description,
  actions,
  tourId,
  titleAs: TitleTag = 'h1',
}: PageHeaderProps) {
  return (
    <header
      className="page-header d-flex justify-content-between align-items-start flex-wrap gap-3"
      data-tour={tourId}
    >
      <div className="page-header-body">
        <TitleTag className="page-header-title">{title}</TitleTag>
        {description ? <p className="page-header-description">{description}</p> : null}
      </div>
      {actions ? <div className="page-header-actions d-flex flex-wrap gap-2">{actions}</div> : null}
    </header>
  );
}

import type { ReactNode } from 'react';

type AlertVariant = 'info' | 'success' | 'warning' | 'danger' | 'neutral';

interface AlertBannerProps {
  variant?: AlertVariant;
  title?: string;
  children: ReactNode;
  className?: string;
}

const variantClass: Record<AlertVariant, string> = {
  info: 'alert-info',
  success: 'alert-success',
  warning: 'alert-warning',
  danger: 'alert-danger',
  neutral: 'alert-light border',
};

export function AlertBanner({
  variant = 'info',
  title,
  children,
  className = '',
}: AlertBannerProps) {
  const role = variant === 'danger' || variant === 'warning' ? 'alert' : 'status';
  return (
    <div className={`alert ${variantClass[variant]} eh-alert-banner ${className}`.trim()} role={role}>
      {title ? (
        <>
          <strong>{title}</strong> {children}
        </>
      ) : (
        children
      )}
    </div>
  );
}

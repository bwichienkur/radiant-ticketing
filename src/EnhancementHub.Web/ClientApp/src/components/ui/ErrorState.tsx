interface ErrorStateProps {
  message: string;
  onRetry?: () => void;
  retryLabel?: string;
  className?: string;
}

export function ErrorState({
  message,
  onRetry,
  retryLabel = 'Try again',
  className = '',
}: ErrorStateProps) {
  return (
    <div
      className={`card-panel border border-danger eh-error-state d-flex flex-wrap justify-content-between align-items-center gap-2 p-3 ${className}`.trim()}
      role="alert"
    >
      <span>{message}</span>
      {onRetry ? (
        <button type="button" className="btn btn-sm btn-outline-danger" onClick={onRetry}>
          {retryLabel}
        </button>
      ) : null}
    </div>
  );
}

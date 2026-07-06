interface ErrorStateProps {
  message: string;
  onRetry?: () => void;
  retryLabel?: string;
}

export function ErrorState({ message, onRetry, retryLabel = 'Try again' }: ErrorStateProps) {
  return (
    <div className="alert alert-danger eh-error-state d-flex flex-wrap justify-content-between align-items-center gap-2" role="alert">
      <span>{message}</span>
      {onRetry ? (
        <button type="button" className="btn btn-sm btn-outline-danger" onClick={onRetry}>
          {retryLabel}
        </button>
      ) : null}
    </div>
  );
}

import { LoadingSkeleton } from '../LoadingSkeleton';

interface LoadingStateProps {
  label?: string;
}

export function LoadingState({ label = 'Loading…' }: LoadingStateProps) {
  return (
    <div aria-busy="true" aria-live="polite">
      <p className="eh-loading-label" role="status">
        {label}
      </p>
      <LoadingSkeleton />
    </div>
  );
}

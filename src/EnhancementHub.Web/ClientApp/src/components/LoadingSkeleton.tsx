export function LoadingSkeleton() {
  return (
    <div className="eh-skeleton" aria-hidden="true">
      <div className="eh-skeleton-line w-50" />
      <div className="eh-skeleton-line w-100" />
      <div className="eh-skeleton-line w-75" />
      <div className="eh-skeleton-line h-lg" />
    </div>
  );
}

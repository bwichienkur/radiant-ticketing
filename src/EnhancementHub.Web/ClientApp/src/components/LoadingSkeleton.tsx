export function LoadingSkeleton() {
  return (
    <div className="placeholder-glow" aria-hidden="true">
      <span className="placeholder col-8 mb-3 d-block" />
      <span className="placeholder col-12 mb-2 d-block" />
      <span className="placeholder col-11 mb-4 d-block" />
      <div className="card-panel p-4">
        <span className="placeholder col-12 d-block" />
      </div>
    </div>
  );
}

interface ListToolbarProps {
  count: number;
  noun?: string;
  filterSummary?: string;
}

export function ListToolbar({ count, noun = 'result', filterSummary }: ListToolbarProps) {
  const label = count === 1 ? noun : `${noun}s`;

  return (
    <div className="eh-list-toolbar d-flex flex-wrap justify-content-between align-items-center gap-2 mb-2">
      <p className="eh-list-toolbar-count mb-0" role="status">
        <strong>{count}</strong> {label}
        {filterSummary ? <span className="text-muted"> · {filterSummary}</span> : null}
      </p>
    </div>
  );
}

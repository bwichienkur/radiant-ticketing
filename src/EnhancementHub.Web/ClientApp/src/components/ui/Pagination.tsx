interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (pageSize: number) => void;
  pageSizeOptions?: number[];
}

export function Pagination({
  page,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 25, 50],
}: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const safePage = Math.min(page, totalPages);
  const start = totalCount === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const end = Math.min(safePage * pageSize, totalCount);

  return (
    <nav className="eh-pagination d-flex flex-wrap justify-content-between align-items-center gap-2" aria-label="Pagination">
      <p className="eh-pagination-summary mb-0 small text-muted" role="status">
        Showing <strong>{start}</strong>–<strong>{end}</strong> of <strong>{totalCount}</strong>
      </p>
      <div className="d-flex flex-wrap align-items-center gap-2">
        {onPageSizeChange ? (
          <label className="d-flex align-items-center gap-2 small mb-0">
            <span className="text-muted">Per page</span>
            <select
              className="form-select form-select-sm eh-pagination-size"
              value={pageSize}
              onChange={(event) => onPageSizeChange(Number(event.target.value))}
              aria-label="Results per page"
            >
              {pageSizeOptions.map((size) => (
                <option key={size} value={size}>
                  {size}
                </option>
              ))}
            </select>
          </label>
        ) : null}
        <div className="btn-group" role="group" aria-label="Page navigation">
          <button
            type="button"
            className="btn btn-sm btn-outline-secondary"
            onClick={() => onPageChange(1)}
            disabled={safePage <= 1}
            aria-label="First page"
          >
            «
          </button>
          <button
            type="button"
            className="btn btn-sm btn-outline-secondary"
            onClick={() => onPageChange(safePage - 1)}
            disabled={safePage <= 1}
            aria-label="Previous page"
          >
            ‹
          </button>
          <span className="btn btn-sm btn-outline-secondary disabled eh-pagination-current" aria-current="page">
            {safePage} / {totalPages}
          </span>
          <button
            type="button"
            className="btn btn-sm btn-outline-secondary"
            onClick={() => onPageChange(safePage + 1)}
            disabled={safePage >= totalPages}
            aria-label="Next page"
          >
            ›
          </button>
          <button
            type="button"
            className="btn btn-sm btn-outline-secondary"
            onClick={() => onPageChange(totalPages)}
            disabled={safePage >= totalPages}
            aria-label="Last page"
          >
            »
          </button>
        </div>
      </div>
    </nav>
  );
}

export function paginateItems<T>(items: T[], page: number, pageSize: number): T[] {
  const start = (page - 1) * pageSize;
  return items.slice(start, start + pageSize);
}

import type { ReactNode } from 'react';

export interface DataColumn<T> {
  id: string;
  header: string;
  cell: (row: T) => ReactNode;
  cellClassName?: string;
}

export interface ResponsiveDataListProps<T> {
  items: T[];
  getRowKey: (row: T) => string;
  columns: DataColumn<T>[];
  renderMobileCard: (row: T) => ReactNode;
  toolbar?: ReactNode;
  className?: string;
}

export function ResponsiveDataList<T>({
  items,
  getRowKey,
  columns,
  renderMobileCard,
  toolbar,
  className = '',
}: ResponsiveDataListProps<T>) {
  return (
    <>
      <div className={`card-panel table-desktop-only ${className}`.trim()}>
        {toolbar}
        <div className="table-responsive">
          <table className="table table-hover table-enterprise mb-0">
            <thead>
              <tr>
                {columns.map((column) => (
                  <th key={column.id} scope="col">
                    {column.header}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {items.map((row) => (
                <tr key={getRowKey(row)}>
                  {columns.map((column) => (
                    <td key={column.id} className={column.cellClassName}>
                      {column.cell(row)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      <div className="cards-mobile-only">
        {items.map((row) => (
          <div key={getRowKey(row)} className="mobile-data-card card-panel p-3 mb-2">
            {renderMobileCard(row)}
          </div>
        ))}
      </div>
    </>
  );
}

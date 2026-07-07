export interface TabItem {
  id: string;
  label: string;
  badge?: string | number;
}

interface TabBarProps {
  tabs: TabItem[];
  activeId: string;
  onChange: (id: string) => void;
  ariaLabel: string;
}

export function TabBar({ tabs, activeId, onChange, ariaLabel }: TabBarProps) {
  return (
    <div className="eh-tab-bar" role="tablist" aria-label={ariaLabel}>
      {tabs.map((tab) => {
        const selected = tab.id === activeId;
        return (
          <button
            key={tab.id}
            type="button"
            role="tab"
            id={`tab-${tab.id}`}
            aria-selected={selected}
            aria-controls={`tabpanel-${tab.id}`}
            className={`eh-tab-bar-item ${selected ? 'active' : ''}`.trim()}
            onClick={() => onChange(tab.id)}
          >
            {tab.label}
            {tab.badge !== undefined ? (
              <span className="eh-tab-bar-badge">{tab.badge}</span>
            ) : null}
          </button>
        );
      })}
    </div>
  );
}
